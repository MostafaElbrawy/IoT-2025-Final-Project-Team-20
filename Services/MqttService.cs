// Services/IMqttService.cs
public interface IMqttService
{
    Task PublishDoorControlAsync(string command);
    Task<string> GetLatestSensorDataAsync();
}

// Services/MqttService.cs (Simple HTTP-based approach)
public class MqttService : IMqttService
{
    private readonly HttpClient _httpClient;

    public MqttService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task PublishDoorControlAsync(string command)
    {
        try
        {
            // This is a simplified approach - in reality you'd use MQTT client library
            // For now, we'll simulate by storing the command
            Console.WriteLine($"Door control command: {command}");

            // You can implement actual MQTT publishing here using MQTTnet library
            // var mqttClient = new MqttFactory().CreateMqttClient();
            // await mqttClient.PublishAsync("esp32/door/control", command);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending door control: {ex.Message}");
        }
    }

    public async Task<string> GetLatestSensorDataAsync()
    {
        try
        {
            // Simulate getting latest sensor data
            // In real implementation, this would subscribe to MQTT topic
            return "Temperature: 25°C, Humidity: 60%, Gas: Normal";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting sensor data: {ex.Message}");
            return "Error retrieving sensor data";
        }
    }
}