from azure.digitaltwins.core import DigitalTwinsClient
from azure.identity import AzureCliCredential
import os 
from dotenv import load_dotenv

load_dotenv()  # Carga las variables del archivo .env

url = os.getenv("AZURE_URL")
credential = AzureCliCredential()
client = DigitalTwinsClient(url, credential)
# Elimina el gemelo antiguo si existe
# try:
#     client.delete_digital_twin("tempSensor1")
#     print("Gemelo anterior eliminado.")
# except Exception as e:
#     print("No existía gemelo anterior o ya fue eliminado.")

# Reinstancia el gemelo con el modelo correcto
patch = {
    "$metadata": {
        "$model": "dtmi:comDT:TemperatureSensor;1"
    },
    "DisplayName": "tempSensor1",
    "Temperature": 0.0
}
twin_id = "tempSensor1"
client.update_digital_twin(twin_id, patch)
print(f"Gemelo {twin_id} actualizado correctamente.")