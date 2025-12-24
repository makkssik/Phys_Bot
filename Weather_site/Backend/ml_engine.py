import pandas as pd
import numpy as np
from sklearn.ensemble import RandomForestClassifier
from sklearn.preprocessing import LabelEncoder
import os
import generate_data

class UnifiedRecommender:
    def __init__(self):
        self.clothing_model = RandomForestClassifier(n_estimators=100, random_state=42)
        self.hobby_model = RandomForestClassifier(n_estimators=100, random_state=42)
        self.hobby_encoder = LabelEncoder()
        self.is_trained = False
        
        self.clothing_labels = {
            0: "Футболка и шорты 👕",
            1: "Футболка и джинсы 👖",
            2: "Легкая кофта/худи 🧥",
            3: "Осенняя куртка/плащ 🧥",
            4: "Зимний пуховик и шапка 🧣",
            5: "Дождевик и непромокаемая обувь ☔️"
        }

        self.hobby_advice_map = {
            0: "✅ Условия отличные! Наслаждайтесь.",
            1: "⛈️ Гроза! Опасно, лучше остаться дома.",
            2: "🥶 Холодно для этого хобби. Утепляйтесь (термобелье)!",
            3: "🥵 Слишком жарко! Осторожно с перегревом.",
            4: "💨 Сильный ветер! Будет сильно мешать или сдувать снаряжение.",
            5: "🌧️ Дождь. Неподходящая погода (промокнете/грязно).",
            6: "❄️ Снег мешает занятию.",
            7: "🌱 Нет снега! А он нужен.",
            8: "⛸️ Риск гололеда/травм. Будьте предельно осторожны.",
            9: "🌫️ Туман или плохая видимость.",
            10: "🏠 На улице шторм, но внутри безопасно. Аккуратнее по дороге.",
            11: "☁️ Облачно. Звезд не видно / Прыгать нельзя.",
            12: "🧗 Камни мокрые и скользкие. Лазать опасно!"
        }

        self.ru_to_en = {
            'бег': 'running', 'спорт': 'crossfit', 'футбол': 'football', 'баскетбол': 'basketball',
            'волейбол': 'volleyball', 'теннис': 'tennis', 'воркаут': 'workout', 'зал': 'gym',
            'фитнес': 'fitness indoor', 'атлетика': 'athletics', 'регби': 'rugby',
            'триатлон': 'triathlon', 'паркур': 'parkour',
            
            'велосипед': 'cycling', 'велик': 'cycling', 'скейт': 'skateboarding', 
            'ролики': 'rollerblading', 'самокат': 'scooter', 'мото': 'motorcycling',
            'мтб': 'mountain biking', 'дрифт': 'drift trike', 'карт': 'go-karting outdoor',
            'мотокросс': 'motocross',
            
            'плавание': 'swimming', 'бассейн': 'swimming pool', 'серфинг': 'surfing', 
            'сап': 'sup boarding', 'лодка': 'kayaking', 'каяк': 'kayaking', 
            'яхта': 'sailing', 'дайвинг': 'diving', 'рыбалка': 'fishing',
            'вейк': 'wakeboarding', 'кайт': 'kitesurfing',
            
            'лыжи': 'skiing', 'горные лыжи': 'alpine skiing', 'сноуборд': 'snowboarding',
            'коньки': 'ice skating', 'хоккей': 'hockey', 'санки': 'sledding', 
            'моржевание': 'winter swimming', 'биатлон': 'biathlon',
            
            'дрон': 'drone flying', 'параплан': 'paragliding', 'стрельба': 'archery',
            'лук': 'archery', 'бадминтон': 'badminton', 'змей': 'kite flying',
            'парашют': 'skydiving', 'фризби': 'frisbee',
            
            'йога': 'yoga outdoor', 'пикник': 'picnic', 'шашлык': 'bbq', 'прогулка': 'walking',
            'поход': 'hiking', 'лес': 'hiking', 'фото': 'photography', 'грибы': 'hiking', 
            'дача': 'gardening', 'сад': 'gardening', 'огород': 'gardening',
            
            'астрономия': 'astronomy', 'телескоп': 'telescope', 'звезды': 'stargazing',
            'скалолазание': 'rock climbing', 'скалы': 'rock climbing',
            'лошади': 'horse riding', 'верховая езда': 'horse riding',
            'пейнтбол': 'paintball', 'страйкбол': 'airsoft', 'лазертаг': 'lasertag outdoor',
            
            'гейминг': 'gaming', 'игры': 'gaming', 'чтение': 'reading indoor', 
            'кино': 'watching movies', 'боулинг': 'bowling', 'готовка': 'cooking',
            'танцы': 'dancing', 'шахматы': 'chess indoor', 'шопинг': 'shopping',
            'бокс': 'boxing', 'бильярд': 'billiards', 'программирование': 'coding'
        }

    def load_and_train(self):
        if not os.path.exists("dataset.csv") or not os.path.exists("hobbies.csv"):
            generate_data.generate_datasets()

        print("🧠 [ML] Training models (Ultimate Edition)...")
        
        df_c = pd.read_csv("dataset.csv")
        self.clothing_model.fit(df_c[['temperature', 'wind_speed', 'weather_code']], df_c['clothing_id'])
        
        df_h = pd.read_csv("hobbies.csv")
        df_h['hobby_enc'] = self.hobby_encoder.fit_transform(df_h['hobby'])
        
        self.hobby_model.fit(df_h[['temperature', 'wind_speed', 'weather_code', 'hobby_enc']], df_h['advice_id'])
        
        self.known_hobbies = set(self.hobby_encoder.classes_)
        self.is_trained = True
        print(f"✅ Trained on {len(self.known_hobbies)} hobbies.")

    def predict_clothing(self, temp, wind, code):
        if not self.is_trained: self.load_and_train()
        pred = self.clothing_model.predict([[temp, wind, code]])[0]
        return self.clothing_labels.get(pred, "Одевайся по погоде")

    def predict_hobby(self, temp, wind, code, user_hobby):
        if not self.is_trained: self.load_and_train()
        
        clean_hobby = user_hobby.lower().strip()
        target_hobby = self.ru_to_en.get(clean_hobby, clean_hobby)

        if target_hobby not in self.known_hobbies:
            found = False
            for en_hobby in self.known_hobbies:
                if target_hobby in en_hobby:
                    target_hobby = en_hobby
                    found = True
                    break
            
            if not found:
                target_hobby = 'walking' 

        try:
            hobby_code = self.hobby_encoder.transform([target_hobby])[0]
            pred_id = self.hobby_model.predict([[temp, wind, code, hobby_code]])[0]
            
            advice_text = self.hobby_advice_map.get(pred_id, "")
            
            if pred_id == 0:
                return f"Для <b>{target_hobby}</b> условия хорошие."
            else:
                return advice_text
                
        except Exception:
            return None

recommender = UnifiedRecommender()