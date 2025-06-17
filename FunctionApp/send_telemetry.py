from paho.mqtt import client as mqtt
import threading, time, os, ssl, json
from dotenv import load_dotenv

# Cargar variables desde .env
load_dotenv()

# Variables de conexión
IOT_HUB_HOSTNAME = os.getenv("IOT_HUB_HOSTNAME")

SENSORS = {
    os.getenv("TEMP_SENSOR"): os.getenv("TEMP_SENSOR_SAS"),
    # os.getenv("PRES_SENSOR"): os.getenv("PRES_SENSOR_SAS"),
    # os.getenv("VIB_SENSOR"): os.getenv("VIB_SENSOR_SAS")
}

stop_event = threading.Event()

def send_mqtt_message(device_id, sas_token): 
    username = f"{IOT_HUB_HOSTNAME}/{device_id}/?api-version=2021-04-12"

    client = mqtt.Client(client_id=device_id, protocol=mqtt.MQTTv311)
    client.username_pw_set(username=username, password=sas_token)
    client.tls_set(cert_reqs=ssl.CERT_REQUIRED, tls_version=ssl.PROTOCOL_TLS)

    def on_connect(client, userdata, flags, rc):
        print(f"[{device_id}] Conectado con código: {rc}")
        if rc == 0:
            print(f"[{device_id}] Conexión exitosa.")
        else:
            print(f"[{device_id}] Error al conectar.")

    client.on_connect = on_connect
    client.connect(IOT_HUB_HOSTNAME, port=8883)
    client.loop_start()

    try:
        while not stop_event.is_set():
            topic = f"devices/{device_id}/messages/events/"
            # payload = f'{{"device": "{device_id}", "value": {round(time.time() % 100, 2)}}}'
            payload = json.dumps({"systemProperties": {
                "iothub-connection-device-id": device_id},
                "body": {"Temperature": round(time.time() % 100, 2)}})
            result = client.publish(topic, payload)
            print(f" [{device_id}] Mensaje enviado: {payload}")
            time.sleep(5)
    finally:
        print(f"\n Deteniendo MQTT para {sensor_id}...")
        time.sleep(1)
        client.loop_stop()
        client.disconnect()
        print(f"[{device_id}] Desconectado.\n")

# Crear y ejecutar threads para cada sensor
threads = []
for sensor_id, sas_token in SENSORS.items():
    thread = threading.Thread(target=send_mqtt_message, args=(sensor_id, sas_token)) 
    thread.start()
    threads.append(thread)

# Mantener el programa corriendo y manejar Ctrl + C
try:
    while True:
        time.sleep(1)
except KeyboardInterrupt:
    print("\n Deteniendo todos los sensores...")
    stop_event.set()
    for thread in threads:
        thread.join()
    print("Todos los sensores han sido detenidos correctamente.")