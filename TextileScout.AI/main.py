from fastapi import FastAPI, UploadFile, File, HTTPException
from ultralytics import YOLO
from PIL import Image
import io

app = FastAPI(title="TextileScout AI Vision Engine")

model = YOLO('yolov8n.pt')

@app.get("/")
def health_check():
    return {"status": "online", "message": "TextileScout Yapay Zeka Servisi Çalışıyor"}

@app.post("/analyze")
async def analyze_image(file: UploadFile = File(...)):
    try:
        contents = await file.read()
        image = Image.open(io.BytesIO(contents))

        width, height = image.size
        
        # Sadece belirgin derecede geniş (Afiş/Banner) olan resimleri engelle (Eni boyunun 1.3 katından büyükse)
        if width > (height * 1.3):
            return {
                "is_clothing": False, 
                "confidence": 0.0, 
                "reason": "Banner/Yatay afis reddedildi."
            }

        results = model(image)

        # 0: insan (person), 26: çanta, 27: kravat
        clothing_related_classes = [0, 26, 27]

        is_clothing = False
        max_confidence = 0.0

        for r in results:
            for box in r.boxes:
                class_id = int(box.cls[0])
                confidence = float(box.conf[0])

                # Güven eşiğini moda modelleri için %50 seviyesine çektik
                if class_id in clothing_related_classes and confidence > 0.50:
                    is_clothing = True
                    if confidence > max_confidence:
                        max_confidence = confidence

        return {
            "is_clothing": is_clothing,
            "confidence": round(max_confidence, 2)
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Görsel işleme hatası: {str(e)}")