import os
import json
from dotenv import load_dotenv
from azure.digitaltwins.core import DigitalTwinsClient
from azure.identity import AzureCliCredential
from azure.core.exceptions import HttpResponseError

# Variable global para el cliente ADT
client = None

def initialize_client():
    global client
    load_dotenv()  # Carga las variables del archivo .env

    url = os.getenv("AZURE_URL")
    credential = AzureCliCredential()
    client = DigitalTwinsClient(url, credential)

    print("Connected to Azure Digital Twins!")

# Cargar modelos desde carpeta
def load_models(folder="./models"):
    models = []
    for filename in os.listdir(folder):
        if filename.endswith(".json"):
            path = os.path.join(folder, filename)
            with open(path, "r") as f:
                content = f.read().strip()
                if content:
                    models.append(content)
                else:
                    print(f"Archivo vacío: {filename}")
    return models

def upload_models(models):
    global client
    if not models:
        print("No se encontraron modelos para subir. Verifica la carpeta './models'")
        return
    try:
        models = [json.loads(model) for model in models]
        client.create_models(models)
        print("Modelos cargados correctamente \n")
    except HttpResponseError as e:
        print(f"Error al subir modelos: {e.message}\n")

# Crear un Digital Twin
def create_digital_twin(twin_id, model_id):
    global client
    twin = {
    "$metadata": {
        "$model": model_id
    },
    "$dtId": twin_id
    }
    
    try:
        client.upsert_digital_twin(twin_id, twin)
        print(f"Gemelo digital '{twin_id}' creado con modelo '{model_id}\n'")
    except HttpResponseError as e:
        print(f"Error al crear gemelo digital '{twin_id}': {e}\n")


# Ahora, crear relaciones para conectar componentes
def create_relationship(source_id, target_id, rel_id, rel_name="hasComponent"):
    global client
    relationship = {
        "$relationshipId": rel_id,
        "$sourceId": source_id,
        "$relationshipName": rel_name,
        "$targetId": target_id
    }
    try:
        client.upsert_relationship(source_id, rel_id, relationship)
        print(f"Relación '{rel_id}' creada: {source_id} --{rel_name}--> {target_id}\n")
    except HttpResponseError as e:
        print(f"Error creando relación {rel_id}: {e.message}\n")

# Construir grafo de ejemplo
def build_graph():
    # Crear avión que agrupa todo
    create_digital_twin("airplane1", "dtmi:comDT:Airplane;1")

    # Crear sensores
    create_digital_twin("tempSensor1", "dtmi:comDT:TemperatureSensor;1")
    create_digital_twin("vibSensor1", "dtmi:comDT:VibrationSensor;1")
    create_digital_twin("presSensor1", "dtmi:comDT:PressureSensor;1")

    # Crear motor con componentes sensores
    # Mediante relaciones porque los componentes son twins separados
    create_digital_twin("engine1", "dtmi:comDT:Engine;1")

    # Crear alas y sistemas
    create_digital_twin("wingLeft1", "dtmi:comDT:Wing;1")
    create_digital_twin("wingRight1", "dtmi:comDT:Wing;1")
    create_digital_twin("electricalSystem1", "dtmi:comDT:ElectricalSystem;1")
    create_digital_twin("hydraulicSystem1", "dtmi:comDT:HydraulicSystem;1")

    
    # Relacionar motor y demás componentes con avión
    create_relationship("airplane1", "engine1", "rel-airplane-engine", "hasEngine")
    create_relationship("airplane1", "wingLeft1", "rel-airplane-wingLeft", "hasWingLeft")
    create_relationship("airplane1", "wingRight1", "rel-airplane-wingRight", "hasWingRight")
    create_relationship("airplane1", "electricalSystem1", "rel-airplane-electrical", "hasElectricalSystem" )
    create_relationship("airplane1", "hydraulicSystem1", "rel-airplane-hydraulic", "hasHydraulicSystem")

    # Relacionar sensores con el motor
    create_relationship("engine1", "tempSensor1", "rel-engine-tempSensor", "hasTemperatureSensor")
    create_relationship("engine1", "vibSensor1", "rel-engine-vibSensor", "hasVibrationSensor")
    create_relationship("engine1", "presSensor1", "rel-engine-presSensor", "hasPressureSensor")

def main():
    initialize_client()

    # Cargar y subir modelos
    models = load_models()
    upload_models(models)
    
    # Crear los twins    
    build_graph()

if __name__ == "__main__":
    main()