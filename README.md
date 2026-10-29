# MultiVoice

Голосовой чат по близости для Minecraft Bedrock: игрок открывает сайт, входит по коду из игры и слышит тех, кто рядом в мире, с 3D-звуком и эффектами (вода, пещеры, предметы). Есть комнаты — как каналы в Discord.

```
Endstone-плагин ──SignalR (JSON)──▶ Backend ◀──SignalR (MessagePack)── Сайт
  позиции, предметы, /mute, /room    .NET 10       микрофон (Opus), динамики
```

| Часть | Где | Что делает |
|---|---|---|
| Backend | `Backend/MultiVoice.Server` | хабы `/api/auth`, `/api/voice`, `/api/minecraft`, `/api/dev`; маршрутизация голоса по чанкам, эффекты, комнаты, SQLite |
| Frontend | `Frontend` | Nuxt 4 SPA: вход, список игроков с громкостью, настройки микрофона, комнаты |
| Plugin | `Plugin` | Endstone-плагин: шлёт позиции и предметы, показывает форму входа, команды `/mute` и `/room`, сервис `multivoice` для других плагинов |

## Запуск для разработки

```sh
# backend: http://localhost:5000
cd Backend/MultiVoice.Server && dotnet run

# frontend: http://localhost:3000
cd Frontend && npm install && npm run dev

# plugin: поставить в venv Endstone и прописать url/secret в config.toml
pip install -e Plugin
```

Проверки:

```sh
cd Frontend && npm run typecheck
cd Frontend && node scripts/jitter-check.mjs
cd Frontend && node scripts/pitch-check.mjs
cd Plugin && python -m pytest -q tests
# end-to-end против запущенного backend (лучше на чистой БД)
cd Frontend && node scripts/smoke.mjs http://localhost:5000
```

## Конфигурация backend

Всё лежит в секции `Voice` файла `appsettings.json` и переопределяется через env (см. ниже).

| Ключ | По умолчанию | Что это |
|---|---|---|
| `McSecret` | `""` | общий секрет с плагином (`secret` в его `config.toml`); пустой — плагин не подключится |
| `Origins` | `[]` | адреса сайта для CORS |
| `DevHub` | `false` | хаб `/api/dev` для тестового бота (`scripts/voicebot.mjs`) |
| `DefaultRange` | `32` | дальность голоса без предметов, в блоках |
| `Effects` | `{}` | библиотека пресетов эффектов |
| `Items` | `[]` | предметы, меняющие голос |

`ConnectionStrings:Settings` — путь к SQLite (громкости игроков, токены входа, запреты). В Docker это `/App/data`, примонтируйте туда volume.

### Дальность и чанки

Голос слышен до `Range` блоков говорящего: полная громкость до `Range × 0.125`, дальше линейно до нуля. Без предмета `Range = DefaultRange`. Размер чанка — наибольшее из `DefaultRange` и всех `Range` предметов, поэтому поиск соседей по 3×3×3 чанкам всегда покрывает самую дальнюю дальность.

### Предметы

Предмет работает, когда он в любой руке или надет. Узнаётся по id типа (`minecraft:goat_horn`, `multipunk:megaphone` из аддона). Если таких предметов несколько, действует один — с наибольшей `Range`, вместе с его громкостью и эффектами.

```json
"Items": [
  { "Id": "minecraft:goat_horn", "Range": 96, "Volume": 1.5, "Effects": [ "megaphone" ] }
]
```

`Items` — массив, а не словарь: в id есть `:`, а это разделитель секций в конфиге .NET.

### Библиотека эффектов

Пресет — набор ключ → значение. Предмет ссылается на пресеты по имени; если ключ встречается в двух пресетах, побеждает последний. Опечатка в ключе или имени пресета роняет сервер при старте с понятной ошибкой.

```json
"Effects": {
  "megaphone": { "Highpass": 500, "Lowpass": 4000, "Distortion": 0.3, "Compressor": 0.8 },
  "radio": { "Highpass": 300, "Lowpass": 3000, "Distortion": 0.15 },
  "goblin": { "Pitch": 5 }
}
```

| Ключ | Диапазон | Эффект |
|---|---|---|
| `Lowpass` | Гц, 20000 = выкл | срезает верха (глухо, «за стеной») |
| `Highpass` | Гц, 0 = выкл | срезает низы (тонко, «в трубке») |
| `Distortion` | 0..1 | перегруз, как дешёвый рупор |
| `Compressor` | 0..1 | пережимает голос: тихое громче, громкое тише |
| `Pitch` | полутона, −24..24 | выше/ниже голос |
| `ReverbWet` / `ReverbDecay` | 0..1 / секунды | реверберация |
| `EchoDelay` / `EchoFeedback` / `EchoWet` | секунды / 0..0.95 / 0..1 | эхо |

Мир добавляет свои эффекты поверх: вода (`Lowpass` 600), пещера (реверб), глубокая пещера (эхо).

## Комнаты

Комната — канал как в Discord: все участники слышат друг друга где угодно в мире, ровно, без 3D и эффектов (работают только громкость и мьют слушателя). С теми, кто не в вашей комнате, по-прежнему работает близость в обе стороны. Если двое из одной комнаты стоят рядом, голос идёт только через комнату — без дубля.

Комнаты временные и приватные: вход по 6-символьному коду, комната исчезает, когда из неё вышел последний. Выход из игры выводит из комнаты.

- Сайт: панель «Комната» — создать, войти по коду, выйти.
- Игра: `/room create`, `/room join <код>`, `/room leave`.

## Запрет говорить

Опы могут запретить игроку говорить — на время или бессрочно; запрет хранится в БД и переживает перезапуски. Права проверяются на сервере: плагин присылает, кто оп, вместе с позицией игрока.

- Игра: `/voiceban <игрок> [минуты]`, `/voiceunban <игрок>`.
- Сайт: у опов в списке игроков есть кнопка запрета с выбором срока.

## Плагин

```
endstone_multivoice/
  plugin.py      сборка: хаб, сервис, executor'ы команд, тик
  commands/      по CommandExecutor на команду: mute, room, ban (voiceban/voiceunban), replies (ответы сервера)
  connection/    SignalR-подключение к /api/minecraft с переподключением
  login/         форма подтверждения входа с сайта
  sync/          позиции, предметы и статус опа — только изменения
  voice/         сервис multivoice с состояниями голоса для других плагинов
```

`config.toml`:

```toml
url = "http://localhost:5000/api/minecraft"
secret = "change-me"   # = Voice:McSecret
period_ticks = 2       # как часто слать позиции (20 тиков = 1 с)
```

К одному backend можно подключить несколько серверов Minecraft. Плагин при каждом запуске создаёт новый id и шлёт его в заголовке `X-Server`; игроков по близости слышно только на своём сервере, комнаты и запреты общие. Плагин без id тоже работает — как отдельный сервер.

Другие плагины могут читать состояние голоса через сервис `multivoice` (`soft_depend = ["multivoice"]`): `get_state(player)` → `off | listening | mic | speaking`, `on_change(callback)`.

## Docker Compose

Образы публикуются в `ghcr.io/mih4n/multivoice/{backend,frontend}`. Готовый пример — [`docker-compose.yml`](docker-compose.yml).

Любой ключ `appsettings.json` задаётся через env: вложенность — `__`, элементы массива нумеруются с 0.

```yaml
environment:
  Voice__McSecret: change-me
  Voice__Origins__0: https://voice.example.com
  Voice__DefaultRange: "32"

  # пресет: Voice__Effects__<имя>__<ключ>
  Voice__Effects__megaphone__Distortion: "0.3"
  Voice__Effects__megaphone__Compressor: "0.8"

  # предмет: Voice__Items__<n>__Id / Range / Volume / Effects__<m>
  Voice__Items__0__Id: minecraft:goat_horn
  Voice__Items__0__Range: "96"
  Voice__Items__0__Volume: "1.5"
  Voice__Items__0__Effects__0: megaphone
```

Env накладывается на `appsettings.json` из образа по ключам, массивы — по индексу: `Voice__Items__0__*` заменяет пример с рогом козы, а пресеты из файла остаются доступны.

Сайту нужен адрес backend, каким его видит браузер: `NUXT_PUBLIC_SERVER_URL`. Без него сайт спросит адрес у пользователя и запомнит его.

## Android-приложение

Тот же сайт в WebView ([Capacitor](https://capacitorjs.com)). Пока голос подключён, `VoiceService` держит уведомление «Голосовая связь подключена», и Android не усыпляет приложение в фоне.

При первом запуске приложение спрашивает адрес сервера. В `Origins` backend нужно добавить `https://localhost`: это origin приложения.

CI собирает подписанный `MultiVoice.apk` на каждый push (артефакт `multivoice-android`) и прикладывает его к релизу. Ключ подписи лежит в секретах репозитория: `ANDROID_KEYSTORE_BASE64` (keystore в base64, alias `multivoice`) и `ANDROID_KEYSTORE_PASSWORD`. Если ключ потерять, новые сборки не встанут поверх старых.

В эмуляторе: `devenv shell -- android-run` (в `devenv.nix` есть SDK и эмулятор; первый запуск качает их долго).

Вручную:

```sh
cd Frontend
npm run android
cd android && ./gradlew assembleDebug # нужен Android SDK в ANDROID_HOME
# APK: android/app/build/outputs/apk/debug/app-debug.apk
```

На NixOS aapt2 из Maven не запускается, берём его из SDK: `-Pandroid.aapt2FromMavenOverride=$ANDROID_HOME/build-tools/36.0.0/aapt2`.
