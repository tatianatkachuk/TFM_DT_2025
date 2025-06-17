from azure.iot.device import IoTHubDeviceClient
import os


# Configurar el dispositivo con clave simétrica
IOT_HUB_CONNECTION_STRING = "HostName=IoTHub-tfm-dt.azure-devices.net;DeviceId=tempSensor1;SharedAccessKey=bJ0UvslvGwUs7VXd20SPM5ySM0W6VPud/9RvmuzA5wQ="

try:
    client = IoTHubDeviceClient.create_from_connection_string(IOT_HUB_CONNECTION_STRING)
    print("Cliente IoT conectado correctamente.")

    def send_test_message():
        message = '{"status": "test_connection", "temperature": 22.5}'
        print(f"Enviando mensaje: {message}")
        client.send_message(message)
        print("Mensaje enviado al IoT Hub con éxito.")

    # Ejecutar prueba
    if __name__ == "__main__":
        send_test_message()

except Exception as e:
    print(f"Error en la conexión: {str(e)}")
