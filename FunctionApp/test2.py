import ssl
import time
from paho.mqtt import client as mqtt 

# Datos del dispositivo
iot_hub_name = "IoTHub-tfm-dt"
device_id = "tempSensor1"
host = f"{iot_hub_name}.azure-devices.net"


username = f"{host}/{device_id}/?api-version=2021-04-12"
key = "HostName=IoTHub-tfm-dt.azure-devices.net;DeviceId=tempSensor1;SharedAccessKey=bJ0UvslvGwUs7VXd20SPM5ySM0W6VPud/9RvmuzA5wQ="  # Extraído desde la cadena de conexión
uri = f"{host}/devices/{device_id}"

# Generar SAS token válido por 1 hora
sas_token = "SharedAccessSignature sr=IoTHub-tfm-dt.azure-devices.net%2Fdevices%2FtempSensor1&sig=d%2FkUc9GzwY9RxTuYkFggxi6YYwwnUWvEiBDJ2yUG0Gw%3D&se=1905881442"

# Crear cliente MQTT
client = mqtt.Client(client_id=device_id, protocol=mqtt.MQTTv311)
client.username_pw_set(username=username, password=sas_token)
client.tls_set(cert_reqs=ssl.CERT_REQUIRED, tls_version=ssl.PROTOCOL_TLS)

# Conectar
def on_connect(client, userdata, flags, rc):
    print("Conectado con código:", rc)
    if rc == 0:
        print("Conexión exitosa.")
    else:
        print("Error al conectar:", rc)

client.on_connect = on_connect
client.connect(host, port=8883)

# Esperar conexión
client.loop_start()
time.sleep(2)

# Publicar mensaje (telemetría)
topic = f"devices/{device_id}/messages/events/"
payload = '{"temperature": 30, "humidity": 20}'
client.publish(topic, payload)
print("Mensaje enviado.")

client.loop_stop()
client.disconnect()

