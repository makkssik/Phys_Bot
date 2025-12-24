import pandas as pd
import numpy as np

HOBBY_CONFIG = {
    'active_team_sport': {
        'hobbies': [
            'football', 'soccer', 'basketball', 'volleyball', 'rugby', 
            'american football', 'baseball', 'cricket', 'lacrosse', 'field hockey',
            'quidditch', 'ultimate frisbee'
        ],
        'rules': {
            'min_temp': 5, 'max_temp': 32, 
            'max_wind': 15, 'rain_forbids': False, 
            'snow_forbids': False, 'ice_risk': True
        }
    },
   
    'fitness_outdoor': {
        'hobbies': [
            'running', 'jogging', 'sprinting', 'marathon', 'parkour', 
            'crossfit', 'workout', 'calisthenics', 'outdoor fitness', 
            'obstacle race', 'triathlon', 'athletics'
        ],
        'rules': {
            'min_temp': -5, 'max_temp': 30, 
            'max_wind': 15, 'rain_forbids': False, 'ice_risk': True
        }
    },
    'wind_critical': {
        'hobbies': [
            'archery', 'badminton', 'table tennis outdoor', 'darts outdoor', 
            'drone flying', 'fpv drone', 'rc planes', 'rc cars', 'kite flying', 
            'disc golf', 'boomerang', 'juggling'
        ],
        'rules': {
            'min_temp': 12, 'max_temp': 35, 
            'max_wind': 7, 
            'rain_forbids': True, 'snow_forbids': True
        }
    },
    'aviation': {
        'hobbies': [
            'paragliding', 'hang gliding', 'skydiving', 'hot air balloon', 
            'gliding', 'paramotor', 'base jumping'
        ],
        'rules': {
            'min_temp': 0, 'max_temp': 30, 
            'max_wind': 5, 
            'rain_forbids': True, 'snow_forbids': True, 'fog_forbids': True,
            'clouds_forbids': True
        }
    },
    'wheels_asphalt': {
        'hobbies': [
            'road bike', 'skateboarding', 'longboarding', 'rollerblading', 
            'roller skating', 'scooter', 'electric scooter', 'go-karting outdoor',
            'drift trike'
        ],
        'rules': {
            'min_temp': 5, 'max_temp': 35, 
            'max_wind': 18, 
            'rain_forbids': True, 'snow_forbids': True, 'ice_risk': True
        }
    },
    'wheels_offroad': {
        'hobbies': [
            'mountain biking', 'bmx', 'motocross', 'enduro', 'atv riding', 
            'jeeping', 'off-roading', 'rally'
        ],
        'rules': {
            'min_temp': 0, 'max_temp': 35, 
            'max_wind': 20, 
            'rain_forbids': False,
            'ice_risk': True
        }
    },
    'water_summer': {
        'hobbies': [
            'swimming', 'open water swimming', 'kayaking', 'canoeing', 'rowing', 
            'sup boarding', 'paddle boarding', 'wakeboarding', 'water skiing', 
            'jet ski', 'diving', 'snorkeling', 'scuba diving', 'flyboard'
        ],
        'rules': {
            'min_temp': 20, 'max_temp': 42, 
            'max_wind': 12, 
            'rain_forbids': False, 'snow_forbids': True
        }
    },
    'water_wind': {
        'hobbies': ['surfing', 'windsurfing', 'kitesurfing', 'sailing', 'yachting'],
        'rules': {
            'min_temp': 15, 'max_temp': 40, 
            'max_wind': 25,
            'rain_forbids': False, 'snow_forbids': True
        }
    },
    'winter_snow': {
        'hobbies': [
            'skiing', 'alpine skiing', 'cross-country skiing', 'snowboarding', 
            'freeride', 'biathlon', 'sledding', 'tubing', 'snowshoeing', 
            'snowmobile', 'making snowman', 'snowball fight', 'winter hiking'
        ],
        'rules': {
            'min_temp': -25, 'max_temp': 3, 
            'max_wind': 15, 
            'rain_forbids': True, 'snow_required': True
        }
    },
    'winter_ice': {
        'hobbies': ['ice skating', 'hockey', 'bandy', 'curling outdoor', 'ice fishing'],
        'rules': {
            'min_temp': -25, 'max_temp': 4, 
            'max_wind': 12, 'rain_forbids': True, 'ice_risk': False
        }
    },
    'chill_outdoor': {
        'hobbies': [
            'walking', 'hiking', 'trekking', 'nordic walking', 'dog walking', 
            'picnic', 'bbq', 'camping', 'glamping', 'fishing', 'spinning'
        ],
        'rules': {
            'min_temp': 10, 'max_temp': 30, 
            'max_wind': 10, 
            'rain_forbids': False 
        }
    },
    'dry_outdoor': {
        'hobbies': [
            'photography', 'landscape photography', 'sketching', 'painting outdoor', 
            'reading outdoor', 'knitting outdoor', 'playing guitar outdoor'
        ],
        'rules': {
            'min_temp': 10, 'max_temp': 30, 
            'max_wind': 8, 
            'rain_forbids': True, 'snow_forbids': True
        }
    },
    'astronomy': {
        'hobbies': ['astronomy', 'stargazing', 'telescope', 'astrophotography'],
        'rules': {
            'min_temp': -15, 'max_temp': 30, 
            'max_wind': 10, 
            'rain_forbids': True, 'fog_forbids': True, 
            'clouds_forbids': True 
        }
    },
    'climbing': {
        'hobbies': ['rock climbing', 'bouldering', 'slackline'],
        'rules': {
            'min_temp': 5, 'max_temp': 28, 
            'max_wind': 12, 
            'rain_forbids': True, 'snow_forbids': True, 
            'wet_rock_risk': True
        }
    },
    'tactical': {
        'hobbies': ['paintball', 'airsoft', 'lasertag outdoor', 'historical reenactment'],
        'rules': {
            'min_temp': 0, 'max_temp': 30,
            'max_wind': 15, 
            'rain_forbids': False
        }
    },
    'animals': {
        'hobbies': ['horse riding', 'equestrian', 'dog training'],
        'rules': {
            'min_temp': -10, 'max_temp': 30, 
            'max_wind': 15, 
            'ice_risk': True 
        }
    },
    'gardening': {
        'hobbies': ['gardening', 'farming', 'landscaping', 'planting'],
        'rules': {
            'min_temp': 5, 'max_temp': 30, 
            'max_wind': 15, 'rain_forbids': True
        }
    },
    'indoor_safe': {
        'hobbies': [
            'gym', 'fitness indoor', 'yoga indoor', 'swimming pool', 'boxing', 
            'martial arts', 'dancing', 'bowling', 'billiards', 'gaming', 
            'esports', 'coding', 'reading indoor', 'watching movies', 
            'board games', 'chess indoor', 'cooking', 'shopping', 'museum',
            'table tennis indoor', 'squash', 'fencing'
        ],
        'rules': {
            'min_temp': -60, 'max_temp': 60, 
            'max_wind': 45, 
            'rain_forbids': False, 'snow_forbids': False
        }
    }
}

def generate_datasets():
    print("🚀 Generating ULTIMATE dataset (100+ hobbies)...")
    np.random.seed(42)
    num_samples = 100000

    temps = np.random.uniform(-35, 42, num_samples)
    winds = np.random.uniform(0, 30, num_samples)
    weather_codes = np.random.choice([0, 1, 2, 3, 45, 51, 61, 63, 71, 73, 75, 95], num_samples)

    clothing_data = []
    for t, w, c in zip(temps, winds, weather_codes):
        feels_like = t - (w * 0.5)
        choice = 0
        is_wet = c in [51, 61, 63, 95]
        
        if is_wet and t > 10: choice = 5
        elif feels_like < -20: choice = 4
        elif feels_like < -5: choice = 4
        elif feels_like < 5: choice = 3
        elif feels_like < 15: choice = 2
        elif feels_like < 25: choice = 1
        else: choice = 0
        clothing_data.append([round(t,1), round(w,1), c, choice])

    pd.DataFrame(clothing_data, columns=['temperature', 'wind_speed', 'weather_code', 'clothing_id']).to_csv("dataset.csv", index=False)

    hobby_data = []
    all_hobbies = []
    for cat in HOBBY_CONFIG.values():
        all_hobbies.extend(cat['hobbies'])

    for i in range(num_samples):
        t = temps[i]
        w = winds[i]
        c = weather_codes[i]
        hobby = np.random.choice(all_hobbies)
        
        rules = {}
        cat_name = ""
        for name, data in HOBBY_CONFIG.items():
            if hobby in data['hobbies']:
                rules = data['rules']
                cat_name = name
                break
        
        advice_id = 0
        
        is_rain = c in [51, 61, 63, 80, 81, 82]
        is_snow = c in [71, 73, 75, 77, 85, 86]
        is_storm = c >= 95
        is_fog = c in [45, 48]
        is_cloudy = c in [2, 3] 
        
        if cat_name == 'indoor_safe':
            if is_storm: advice_id = 10 
            else: advice_id = 0 
        else:
            if is_storm: advice_id = 1
            elif t < rules.get('min_temp', -50): advice_id = 2
            elif t > rules.get('max_temp', 100): advice_id = 3
            elif w > rules.get('max_wind', 99): advice_id = 4
            elif is_rain and rules.get('rain_forbids', False): advice_id = 5
            elif is_snow and rules.get('snow_forbids', False): advice_id = 6
            elif rules.get('snow_required', False) and not is_snow and t > 0: advice_id = 7
            elif rules.get('ice_risk', False) and (t < 2 and (is_rain or is_snow or c<=2)): advice_id = 8
            elif is_fog and rules.get('fog_forbids', False): advice_id = 9
            elif is_cloudy and rules.get('clouds_forbids', False): advice_id = 11
            elif (is_rain or is_fog) and rules.get('wet_rock_risk', False): advice_id = 12

        hobby_data.append([round(t,1), round(w,1), c, hobby, advice_id])

    pd.DataFrame(hobby_data, columns=['temperature', 'wind_speed', 'weather_code', 'hobby', 'advice_id']).to_csv("hobbies.csv", index=False)
    print(f"✅ Generated {len(hobby_data)} samples. Knowledge base: {len(all_hobbies)} hobbies.")

if __name__ == "__main__":
    generate_datasets()