from azure.digitaltwins.core import DigitalTwinsClient 
from azure.identity import AzureCliCredential
import os 
from dotenv import load_dotenv
 
load_dotenv()  # Carga las variables del archivo .env

url = os.getenv("AZURE_URL")
credential = AzureCliCredential()
client = DigitalTwinsClient(url, credential)

print("Connected to Azure Digital Twins!")

# ID del gemelo que quieres actualizar
twin_id = "tempSensor1"

# Payload PATCH para actualizar la propiedad 'temperature'
patch = [
    {
        "op": "replace",   # operación: reemplazar valor
        "path": "/temperature",  # propiedad a actualizar
        "value": 25.7     # nuevo valor
    }
]
try:
    # Ejecutar la actualización
    client.update_digital_twin(twin_id, patch)
    print(f"Gemelo {twin_id} actualizado correctamente.")
except Exception as e:
    print(str(e))
