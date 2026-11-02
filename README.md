# 🚌 Dozor Bus Stop Logger (Збір статистичної вибірки)

C# .NET 8 фоновий сервіс для автоматичного збору емпіричної вибірки реальних інтервалів руху маршруток через Dozor Tech API (`city.dozor.tech`). Готовий до запуску в **Azure Container Apps** або локально через Docker / .NET.

---

## 🎯 Як фіксується зупинка

- **В'їзд у зону зупинки**: факт перетину геозони (радіус 40м) вважається моментом прибуття / зупинки на станції.
- **Азимут (напрямок)**: відсікає зустрічні маршрутки по іншій стороні дороги.
- **Захист від дублікатів**: один і той самий автобус не фіксується повторно під час перебування на зупинці (кулдаун 5 хвилин).
- **Вибірка інтервалів**: автоматично рахується інтервал у хвилинах між прибуттям попереднього та поточного автобуса.

---

## 📋 Формат вихідних даних

### 1. Консольні логи:
Лаконічний вивід без зайвих деталей:
```text
[2026-10-30 07:15:32] Зупинка #1 | 718 АІ 8159 ОІ | Google Drive: Успішно
[2026-10-30 07:30:15] Зупинка #2 | Інтервал: 14.7 хв | 753 AC 7452 HP | Google Drive: Успішно
[2026-10-30 07:44:20] Зупинка #3 | Інтервал: 14.1 хв | 761 КА 4266 РА | Google Drive: Успішно
```

### 2. CSV файл (`logs/bus_stop_intervals.csv` та синхронізація на Google Диск):
Містить повну вибірку спостережень, яка слугує доказом реального збору даних:
```csv
№,Час зупинки,Інтервал (хв),Номер автобуса,ID трекера,Дистанція (м),Швидкість (км/год)
1,2026-10-30 07:15:32,-,"718 АІ 8159 ОІ",19447,18.4,14.0
2,2026-10-30 07:30:15,14.7,"753 AC 7452 HP",16478,21.0,12.0
...
```

---

## ⚙️ Налаштування (`.env`)

```env
DOZOR_URL=https://city.dozor.tech/data?t=2&p={0}
ROUTE_ID=2504

TARGET_LAT=49.433426
TARGET_LNG=32.054128
STOP_RADIUS_METERS=40
EXPECTED_AZIMUTH=220
AZIMUTH_TOLERANCE=50
POLL_INTERVAL_SECONDS=5

LOCAL_LOG_FILE_PATH=logs/bus_stop_intervals.csv

# Google Drive (залиште порожнім, якщо зберігати лише локально)
GOOGLE_DRIVE_FOLDER_ID=
GOOGLE_CREDENTIALS_JSON=
```

---

## 🚀 Деплой в Azure Container Apps

```bash
az login

az containerapp up \
  --name dozor-tracker \
  --resource-group rg-dozor \
  --location polandcentral \
  --source . \
  --min-replicas 1 \
  --max-replicas 1 \
  --env-vars \
    ROUTE_ID="2504" \
    TARGET_LAT="49.433426" \
    TARGET_LNG="32.054128" \
    STOP_RADIUS_METERS="40" \
    EXPECTED_AZIMUTH="220" \
    POLL_INTERVAL_SECONDS="5" \
    GOOGLE_DRIVE_FOLDER_ID="ID_ВАШОЇ_ПАПКИ" \
    GOOGLE_CREDENTIALS_JSON="JSON_КЛЮЧ"
```
