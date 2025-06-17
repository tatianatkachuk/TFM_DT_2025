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


try:
    client.delete_digital_twin(twin_id)
    print(f"Gemelo digital '{twin_id}' eliminado correctamente.")
    
except Exception as e:
    print(f"Error eliminando el gemelo digital: {e}")
