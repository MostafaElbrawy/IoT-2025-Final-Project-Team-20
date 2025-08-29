// js/app.js

// Supabase Configuration (Replace with your actual values)
const SUPABASE_URL = 'https://kvmvstfomzzbzcggzkme.supabase.co';
const SUPABASE_ANON_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imt2bXZzdGZvbXp6YnpjZ2d6a21lIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTYyNzE3NDYsImV4cCI6MjA3MTg0Nzc0Nn0.Ds3-2fiwYXKYg4IiOA3uQ8tgEErCY7a3p6d53j_kFIA';
// js/app.js

// Supabase Configuration (Replace with your actual values)
const SUPABASE_URL = 'https://your-project.supabase.co';
const SUPABASE_ANON_KEY = 'your-anon-key-here';

// Simple user session management
let currentUser = null;

// Initialize app when page loads
document.addEventListener('DOMContentLoaded', function () {
    // Check if user is logged in
    currentUser = localStorage.getItem('currentUser');
    updateNavigation();

    // Auto-refresh dashboard data every 30 seconds if on dashboard page
    if (window.location.pathname.includes('Dashboard') || window.location.pathname.includes('dashboard.html')) {
        setInterval(loadDashboardData, 30000);
        loadDashboardData();
    }
});

// Update navigation based on login status
function updateNavigation() {
    const navLinks = document.querySelector('.nav-links');
    if (!navLinks) return;

    if (currentUser) {
        navLinks.innerHTML = `
            <li><a href="/Home/Index">Home</a></li>
            <li><a href="/Home/Dashboard">Dashboard</a></li>
            <li><a href="/Control/Index">Control</a></li>
            <li><a href="/PeopleDetection/Index">People Detection</a></li>
            <li><a href="/Alert/Index">Alerts</a></li>
            <li><a href="/History/Index">History</a></li>
            <li><a href="#" onclick="logout(); return false;">Logout (${currentUser})</a></li>
        `;
    } else {
        navLinks.innerHTML = `
            <li><a href="/Home/Index">Home</a></li>
            <li><a href="/Account/Login">Login</a></li>
        `;
    }
}


// Login function
async function login(email, password) {
    try {
        // Simple authentication - in real app, use Supabase Auth
        if (email && password && email.length > 0 && password.length > 0) {
            currentUser = email;
            localStorage.setItem('currentUser', email);
            localStorage.setItem('loginTime', new Date().toISOString());

            // Log access
            await logAccess('login', email, true);

            // Show success message
            showMessage('Login successful! Redirecting...', 'success');

            // Update navigation immediately
            updateNavigation();

            // Redirect to dashboard after short delay
            setTimeout(() => {
                window.location.href = '/Home/Dashboard';
            }, 1000);

            return true;
        } else {
            showMessage('Please enter valid email and password', 'error');
            return false;
        }
    } catch (error) {
        console.error('Login error:', error);
        showMessage('Login failed. Please try again.', 'error');
        return false;
    }
}

// Logout function
function logout() {
    if (currentUser) {
        logAccess('logout', currentUser, true);
    }

    // Clear all session data
    currentUser = null;
    localStorage.removeItem('currentUser');
    localStorage.removeItem('loginTime');

    // Update navigation immediately
    updateNavigation();

    // Show logout message
    showMessage('Logged out successfully', 'info');

    // Redirect to home
    setTimeout(() => {
        window.location.href = '/Home/Index';
    }, 1000);
}

// Check if user is authenticated
function requireAuth() {
    // First check localStorage
    const storedUser = localStorage.getItem('currentUser');

    if (!storedUser) {
        // No stored user, redirect to login
        window.location.href = '/Account/Login';
        return false;
    }

    // Update current user if it's not set
    if (!currentUser) {
        currentUser = storedUser;
        updateNavigation();
    }

    return true;
}

// Check session validity (optional - for auto-logout after inactivity)
function checkSessionValidity() {
    const loginTime = localStorage.getItem('loginTime');
    if (loginTime) {
        const sessionAge = new Date() - new Date(loginTime);
        const maxSessionAge = 24 * 60 * 60 * 1000; // 24 hours

        if (sessionAge > maxSessionAge) {
            // Session expired
            logout();
            showMessage('Session expired. Please login again.', 'warning');
            return false;
        }
    }
    return true;
}

// Supabase API Functions (Simplified for demo)
async function makeSupabaseRequest(table, method = 'GET', data = null) {
    try {
        const url = `${SUPABASE_URL}/rest/v1/${table}`;
        const options = {
            method: method,
            headers: {
                'apikey': SUPABASE_ANON_KEY,
                'Authorization': `Bearer ${SUPABASE_ANON_KEY}`,
                'Content-Type': 'application/json'
            }
        };

        if (data && method !== 'GET') {
            options.body = JSON.stringify(data);
        }

        const response = await fetch(url, options);
        if (response.ok) {
            return await response.json();
        }
        throw new Error('API request failed');
    } catch (error) {
        console.error('Supabase request error:', error);
        // Return mock data for demo
        return getMockData(table);
    }
}



// Load dashboard data
async function loadDashboardData() {
    if (!requireAuth()) return;

    try {
        // Show loading state
        showLoading(true);

        // Load sensor readings
       
        // Load alerts
        const alerts = await makeSupabaseRequest('alerts?resolved=eq.false&order=timestamp.desc');
        updateAlertsDisplay(alerts);

      

        showLoading(false);
    } catch (error) {
        console.error('Error loading dashboard data:', error);
        showLoading(false);
    }
}




// Door control functions
async function controlDoor(action) {
    if (!requireAuth()) return;

    try {
        showLoading(true);

        // Simulate MQTT publish (replace with actual MQTT implementation)
        console.log(`Publishing to esp32/door/control: ${action}`);

        // Log the action
        await logAccess(`door_${action.toLowerCase()}`, currentUser, true);

        // Update door status display
        updateDoorStatus(action);

        // Show success message
        showMessage(`Door ${action.toLowerCase()} command sent successfully!`, 'success');

        showLoading(false);
    } catch (error) {
        console.error('Door control error:', error);
        showMessage('Failed to send door control command', 'error');
        showLoading(false);
    }
}

// Update door status display
function updateDoorStatus(status) {
    const statusElement = document.getElementById('door-status');
    if (statusElement) {
        statusElement.textContent = `Door is ${status.toLowerCase()}`;
        statusElement.className = `door-status ${status === 'OPEN' ? 'door-open' : 'door-closed'}`;
    }
}



// Utility functions
function showLoading(show) {
    const elements = document.querySelectorAll('.card, .btn');
    elements.forEach(el => {
        if (show) {
            el.classList.add('loading');
        } else {
            el.classList.remove('loading');
        }
    });
}

function showMessage(message, type = 'info') {
    const messageDiv = document.createElement('div');
    messageDiv.className = `alert alert-${type === 'error' ? 'danger' : type === 'success' ? 'success' : 'info'}`;
    messageDiv.textContent = message;

    const container = document.querySelector('.container');
    container.insertBefore(messageDiv, container.firstChild);

    // Remove message after 5 seconds
    setTimeout(() => {
        messageDiv.remove();
    }, 5000);
}