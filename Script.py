import socket
import sys
import cv2
import torch
import numpy as np
from PIL import Image
from facenet_pytorch import InceptionResnetV1, MTCNN
import joblib
import time
from collections import deque
from PyQt5.QtWidgets import QApplication, QLabel, QWidget, QVBoxLayout
from PyQt5.QtGui import QImage, QPixmap
from PyQt5.QtCore import QTimer
import requests


# =======================
# Configuration
# =======================
esp32_ip = "192.168.1.53"   # <-- غيّرها للـ IP اللي طلعلك من Serial Monitor
esp32_port = 8080

FRAME_WINDOW = 11
ACCEPT_THRESHOLD = 6
UNKNOWN_THRESHOLD = 6
CONF_THRESH = 0.80


# =======================
# Supabase config
# =======================
SUPABASE_URL = "https://kvmvstfomzzbzcggzkme.supabase.co"
SUPABASE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imt2bXZzdGZvbXp6YnpjZ2d6a21lIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTYyNzE3NDYsImV4cCI6MjA3MTg0Nzc0Nn0.Ds3-2fiwYXKYg4IiOA3uQ8tgEErCY7a3p6d53j_kFIA"


def log_detection_to_supabase(person_id, person_name, confidence_score, detection_status, action_taken, camera_id="CAM_001"):
    url = f"{SUPABASE_URL}/rest/v1/people_detections"
    headers = {
        "apikey": SUPABASE_KEY,
        "Authorization": f"Bearer {SUPABASE_KEY}",
        "Content-Type": "application/json",
        "Prefer": "return=minimal"
    }
    payload = {
        "person_id": person_id,   # None لو مش معروف
        "person_name": person_name,
        "confidence_score": round(confidence_score * 100, 2),
        "detection_status": detection_status,
        "action_taken": action_taken,
        "camera_id": camera_id
    }
    try:
        r = requests.post(url, headers=headers, json=payload, timeout=3)
        if r.status_code not in (200, 201, 204):
            print("Supabase insert error:", r.status_code, r.text)
        else:
            print("Logged to Supabase:", person_name, detection_status)
    except Exception as e:
        print("Supabase error:", e)


# =======================
# Helper: send message to ESP32
# =======================
def send_to_esp32(message, timeout=0.8):
    try:
        with socket.create_connection((esp32_ip, esp32_port), timeout=timeout) as s:
            s.sendall((message.strip() + "\n").encode("utf-8"))
            try:
                s.settimeout(0.5)
                resp = s.recv(256).decode(errors="ignore").strip()
                if resp:
                    print("ESP32 replied:", resp)
            except socket.timeout:
                pass
        return True
    except Exception as e:
        print("Send error:", e)
        return False


# =======================
# Load models
# =======================
device = 'cuda' if torch.cuda.is_available() else 'cpu'
print("Using device:", device)

mtcnn = MTCNN(image_size=160, margin=20, device=device)
resnet = InceptionResnetV1(pretrained='vggface2').eval().to(device)
clf = joblib.load("face_svm.pkl")


# =======================
# Camera + frame queue
# =======================
cap = cv2.VideoCapture(0)
if not cap.isOpened():
    print("Cannot open camera")
    sys.exit(1)

frame_queue = deque(maxlen=FRAME_WINDOW)

# state vars
state = "waiting"
state_start_time = 0.0
current_name = ""


# =======================
# PyQt GUI
# =======================
class VideoWindow(QWidget):
    def _init_(self):
        super()._init_()
        self.setWindowTitle("Face Recognition")
        self.image_label = QLabel()

        layout = QVBoxLayout()
        layout.addWidget(self.image_label)
        self.setLayout(layout)

        self.timer = QTimer()
        self.timer.timeout.connect(self.update_frame)
        self.timer.start(30)

    def update_frame(self):
        global state, state_start_time, current_name

        ret, frame = cap.read()
        if not ret:
            return

        img_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        img_pil = Image.fromarray(img_rgb)

        identity = "No Face"
        confidence = 0.0

        face_tensor = mtcnn(img_pil)
        if face_tensor is not None:
            with torch.no_grad():
                emb = resnet(face_tensor.unsqueeze(0).to(device)).detach().cpu().numpy()[0]
                probs = clf.predict_proba([emb])[0]
                pred_idx = int(np.argmax(probs))
                pred_name = str(clf.classes_[pred_idx])
                confidence = float(np.max(probs))
                identity = pred_name if confidence >= CONF_THRESH else "Unknown"

        frame_queue.append((identity, confidence))

        if len(frame_queue) == FRAME_WINDOW and state == "waiting":
            accepted_count = sum(1 for n, c in frame_queue if (n != "Unknown" and n != "No Face" and c >= CONF_THRESH))
            unknown_count = sum(1 for n, c in frame_queue if n == "Unknown")

            if accepted_count >= ACCEPT_THRESHOLD:
                names = [n for n, c in frame_queue if n not in ("Unknown", "No Face") and c >= CONF_THRESH]
                if names:
                    current_name = max(set(names), key=names.count)
                else:
                    current_name = "Unknown"

                ok = send_to_esp32(f"Accepted: {current_name}")
                print("Sent Accepted:", current_name, "OK?", ok)

                # ✅ سجل في Supabase
                log_detection_to_supabase(
                    person_id=None,
                    person_name=current_name,
                    confidence_score=confidence,
                    detection_status="authorized",
                    action_taken="door_opened"
                )

                state = "accepted"
                state_start_time = time.time()
                frame_queue.clear()

            elif unknown_count >= UNKNOWN_THRESHOLD:
                ok = send_to_esp32("UNKNOWN")
                print("Sent UNKNOWN", ok)

                # ✅ سجل في Supabase
                log_detection_to_supabase(
                    person_id=None,
                    person_name="Unknown Person",
                    confidence_score=confidence,
                    detection_status="unknown",
                    action_taken="alert_sent"
                )

                state = "unknown"
                state_start_time = time.time()
                frame_queue.clear()

        if state in ("accepted", "unknown"):
            if time.time() - state_start_time >= 7.0:
                state = "waiting"
                current_name = ""
                frame_queue.clear()

        if identity == "No Face":
            overlay = "No Face"
            color = (200, 200, 200)
        elif identity == "Unknown":
            overlay = "Unknown"
            color = (0, 0, 255)
        else:
            overlay = f"{identity} ({confidence*100:.1f}%)"
            color = (0, 255, 0)

        cv2.putText(frame, overlay, (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 1, color, 2)

        h, w, ch = frame.shape
        bytes_per_line = ch * w
        qt_image = QImage(frame.data, w, h, bytes_per_line, QImage.Format_BGR888)
        self.image_label.setPixmap(QPixmap.fromImage(qt_image))


# =======================
# Run App
# =======================
if _name_ == "_main_":
    app = QApplication(sys.argv)
    window = VideoWindow()
    window.show()
    try:
        sys.exit(app.exec_())
    finally:
        cap.release()
        cv2.destroyAllWindows()
