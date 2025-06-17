import os
import json
from dotenv import load_dotenv
from azure.digitaltwins.core import DigitalTwinsClient
from azure.identity import AzureCliCredential 

# Variable global para el cliente ADT
client = None

def initialize_client():
    global client
    load_dotenv()  # Carga las variables del archivo .env

    url = os.getenv("AZURE_URL")
    credential = AzureCliCredential()
    client = DigitalTwinsClient(url, credential)

    print("Connected to Azure Digital Twins!")

initialize_client()
twins = client.query_twins("SELECT * FROM DIGITALTWINS")
for twin in twins:
    client.delete_digital_twin(twin["$dtId"])
    print(f"Gemelo digital '{twin['$dtId']}' eliminado correctamente.")
