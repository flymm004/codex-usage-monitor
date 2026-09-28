using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CodexUsageMonitor
{
    internal static class UiText
    {
        // Column order also defines the order shown in both language menus.
        public static readonly string[] LanguageCodes = { "en", "zh-CN", "zh-TW", "ja", "ko", "es", "de", "fr" };
        private static readonly Dictionary<string, string[]> Strings = new Dictionary<string, string[]>
        {
            {"title", new[] {"Codex Usage", "Codex 用量", "Codex 用量", "Codex 使用量", "Codex 사용량", "Uso de Codex", "Codex-Nutzung", "Usage Codex"}},
            {"connecting", new[] {"Connecting to Codex…", "正在连接本机 Codex…", "正在連線本機 Codex…", "Codex に接続中…", "로컬 Codex에 연결 중…", "Conectando con Codex…", "Verbinde mit Codex…", "Connexion à Codex…"}},
            {"connected", new[] {"Connected to local Codex", "已连接本机 Codex", "已連線本機 Codex", "ローカル Codex に接続済み", "로컬 Codex에 연결됨", "Conectado a Codex local", "Mit lokalem Codex verbunden", "Connecté à Codex local"}},
            {"reading", new[] {"Loading", "读取中", "讀取中", "読込中", "불러오는 중", "Cargando", "Lädt…", "Chargement…"}},
            {"waiting", new[] {"Waiting…", "等待更新…", "等待更新…", "更新待ち…", "업데이트 대기 중…", "En espera…", "Wartet…", "En attente…"}},
            {"loadingUsage", new[] {"Reading account limits…", "正在读取账户额度…", "正在讀取帳戶額度…", "アカウントの上限を取得中…", "계정 한도를 읽는 중…", "Leyendo límites de la cuenta…", "Kontingente werden geladen…", "Lecture des limites du compte…"}},
            {"readingResets", new[] {"Checking reset credits…", "正在读取重置次数…", "正在讀取重設次數…", "リセット回数を取得中…", "재설정 횟수를 확인 중…", "Consultando reinicios disponibles…", "Verfügbare Zurücksetzungen…", "Vérification des réinitialisations…"}},
            {"refresh", new[] {"Refresh", "刷新", "重新整理", "更新", "새로고침", "Actualizar", "Neu laden", "Actualiser"}},
            {"info", new[] {"Info", "说明", "說明", "詳細", "정보", "Info", "Info", "Info"}},
            {"aboutTitle", new[] {"About usage", "关于额度显示", "關於額度顯示", "使用量について", "사용량 안내", "Acerca del uso", "Zur Nutzung", "À propos de l’usage"}},
            {"aboutBody", new[] {
                "This panel reads limit percentages, window lengths, and reset times from the local Codex app server.\r\n\r\nUsage varies by model and task, so it does not estimate how many tasks remain. It is read-only and does not store sign-in tokens.",
                "此面板通过本机 Codex 读取账户返回的额度比例、窗口长度和重置时间。\r\n\r\n实际消耗会随模型和任务变化，因此不估算还能完成多少次任务。程序只读，不保存登录令牌。",
                "此面板會透過本機 Codex 讀取帳戶額度比例、週期長度與重設時間。\r\n\r\n實際用量會隨模型和工作而變化，因此不估算還能完成多少項工作。程式僅供讀取，不會儲存登入權杖。",
                "このパネルはローカルの Codex から利用枠の割合、期間、リセット時刻を取得します。\r\n\r\n消費量はモデルや作業によって変わるため、残りの作業回数は推定しません。読み取り専用で、ログイントークンは保存しません。",
                "이 패널은 로컬 Codex에서 한도 비율, 기간, 재설정 시간을 읽습니다.\r\n\r\n사용량은 모델과 작업에 따라 달라지므로 남은 작업 수를 추정하지 않습니다. 읽기 전용이며 로그인 토큰을 저장하지 않습니다.",
                "Este panel consulta en Codex local los porcentajes, la duración de los períodos y las horas de reinicio.\r\n\r\nEl consumo varía según el modelo y la tarea, así que no calcula cuántas tareas quedan. Es de solo lectura y no guarda tokens de inicio de sesión.",
                "Dieses Fenster liest Auslastung, Zeiträume und Zurücksetzungszeiten vom lokalen Codex-App-Server.\r\n\r\nDer Verbrauch hängt von Modell und Aufgabe ab. Daher wird nicht geschätzt, wie viele Aufgaben verbleiben. Die App ist schreibgeschützt und speichert keine Anmeldetokens.",
                "Ce panneau lit les pourcentages, les périodes et les heures de réinitialisation depuis le serveur Codex local.\r\n\r\nLa consommation varie selon le modèle et la tâche ; le nombre de tâches restantes n’est donc pas estimé. L’application est en lecture seule et ne stocke aucun jeton de connexion."}},
            {"updating", new[] {"Updating…", "更新中…", "更新中…", "更新中…", "업데이트 중…", "Actualizando…", "Wird aktualisiert…", "Mise à jour…"}},
            {"updatedAt", new[] {"Updated {0}", "{0} 更新", "{0} 更新", "{0} 更新", "{0} 업데이트", "Actualizado {0}", "{0} aktualisiert", "Mis à jour à {0}"}},
            {"readFailed", new[] {"Read failed", "读取失败", "讀取失敗", "取得失敗", "읽기 실패", "Error de lectura", "Lesefehler", "Échec de lecture"}},
            {"notConnected", new[] {"Offline", "未连接", "未連線", "未接続", "연결 안 됨", "Sin conexión", "Nicht verbunden", "Hors ligne"}},
            {"retryHint", new[] {"Select Refresh to retry.", "点击“刷新”重试。", "按一下「重新整理」再試一次。", "「更新」を押して再試行してください。", "‘새로고침’을 눌러 다시 시도하세요.", "Pulsa «Actualizar» para reintentar.", "Zum erneuten Versuch auf „Aktualisieren“ klicken.", "Cliquez sur « Actualiser » pour réessayer."}},
            {"noResets", new[] {"No resets available", "暂无可用重置", "目前沒有可用重設", "利用可能なリセットなし", "사용 가능한 재설정 없음", "No hay reinicios disponibles", "Keine Zurücksetzungen verfügbar", "Aucune réinitialisation disponible"}},
            {"resetAt", new[] {"Resets {0}", "重置于 {0}", "重設於 {0}", "{0}にリセット", "{0}에 재설정", "Se restablece {0}", "Wird um {0} zurückgesetzt", "Réinitialisation à {0}"}},
            {"trayReading", new[] {"Codex · Loading", "Codex · 正在读取", "Codex · 正在讀取", "Codex · 読込中", "Codex · 불러오는 중", "Codex · Cargando", "Codex · Lädt…", "Codex · Chargement"}},
            {"trayFailed", new[] {"Codex · Read failed", "Codex · 读取失败", "Codex · 讀取失敗", "Codex · 取得失敗", "Codex · 읽기 실패", "Codex · Error de lectura", "Codex · Lesefehler", "Codex · Échec de lecture"}},
            {"menuShow", new[] {"Show usage", "显示用量", "顯示用量", "使用量を表示", "사용량 보기", "Mostrar uso", "Nutzung anzeigen", "Afficher l’utilisation"}},
            {"menuRefresh", new[] {"Refresh now", "立即刷新", "立即重新整理", "今すぐ更新", "지금 새로고침", "Actualizar ahora", "Jetzt aktualisieren", "Actualiser"}},
            {"menuLanguage", new[] {"Language", "语言", "語言", "言語", "언어", "Idioma", "Sprache", "Langue"}},
            {"menuExit", new[] {"Exit", "退出", "結束", "終了", "종료", "Salir", "Beenden", "Quitter"}},
            {"systemLanguage", new[] {"System default", "跟随系统", "依照系統", "システム設定", "시스템 기본값", "Idioma del sistema", "Systemstandard", "Langue du système"}},
            {"languageTooltip", new[] {"Change language", "切换语言", "切換語言", "言語を変更", "언어 변경", "Cambiar idioma", "Sprache ändern", "Changer de langue"}},
            {"closeTooltip", new[] {"Close panel", "关闭面板", "關閉面板", "パネルを閉じる", "패널 닫기", "Cerrar panel", "Fenster schließen", "Fermer le panneau"}},
            {"refreshTooltip", new[] {"Refresh usage now", "立即刷新额度", "立即重新整理額度", "使用量を今すぐ更新", "사용량 새로고침", "Actualizar uso", "Nutzung jetzt aktualisieren", "Actualiser l’utilisation"}},
            {"infoTooltip", new[] {"About these limits", "查看额度说明", "查看額度說明", "利用枠の説明", "한도 안내", "Acerca de estos límites", "Informationen zu den Kontingenten", "À propos de ces limites"}},
            {"lowTitle", new[] {"Codex usage is low", "Codex 额度偏低", "Codex 額度偏低", "Codex の残量が少なくなりました", "Codex 잔여량이 적습니다", "Queda poco uso de Codex", "Codex-Kontingent fast aufgebraucht", "Quota Codex bientôt épuisé"}},
            {"lowBody", new[] {
                "A limit is below 20%. Open the tray icon to see its reset time.",
                "有一个额度窗口低于 20%。点击托盘图标查看重置时间。",
                "有一個額度週期低於 20%。按一下系統匣圖示查看重設時間。",
                "残量が 20% 未満の枠があります。トレイアイコンでリセット時刻を確認できます。",
                "한도 잔여량이 20% 미만입니다. 트레이 아이콘에서 재설정 시간을 확인하세요.",
                "Queda menos del 20 % en un período. Abre el icono de la bandeja para ver cuándo se reinicia.",
                "Ein Kontingent liegt unter 20 %. Über das Infobereichsymbol siehst du die Zurücksetzungszeit.",
                "Un quota est inférieur à 20 %. Ouvrez l’icône de la zone de notification pour voir l’heure de réinitialisation."}},
            {"errorCliMissing", new[] {"Codex CLI was not found. Check your installation.", "找不到 Codex CLI。请检查 Codex 安装。", "找不到 Codex CLI。請檢查 Codex 安裝。", "Codex CLI が見つかりません。インストールを確認してください。", "Codex CLI를 찾을 수 없습니다. 설치를 확인하세요.", "No se encontró Codex CLI. Comprueba la instalación.", "Codex CLI wurde nicht gefunden. Prüfe die Installation.", "Codex CLI introuvable. Vérifiez l’installation."}},
            {"errorCliStart", new[] {"Could not start the local Codex CLI.", "无法启动本机 Codex CLI。", "無法啟動本機 Codex CLI。", "ローカルの Codex CLI を起動できません。", "로컬 Codex CLI를 시작할 수 없습니다.", "No se pudo iniciar Codex CLI local.", "Die lokale Codex CLI konnte nicht gestartet werden.", "Impossible de démarrer le Codex CLI local."}},
            {"errorServerStart", new[] {"Could not start the local Codex service.", "无法启动本机 Codex 服务。", "無法啟動本機 Codex 服務。", "ローカルの Codex サービスを起動できません。", "로컬 Codex 서비스를 시작할 수 없습니다.", "No se pudo iniciar el servicio local de Codex.", "Der lokale Codex-Dienst konnte nicht gestartet werden.", "Impossible de démarrer le service Codex local."}},
            {"errorNoAccount", new[] {"Codex is not signed in to a ChatGPT account.", "Codex 尚未登录 ChatGPT 账户。", "Codex 尚未登入 ChatGPT 帳戶。", "Codex で ChatGPT アカウントにログインしていません。", "Codex에서 ChatGPT 계정에 로그인하지 않았습니다.", "Codex no ha iniciado sesión en una cuenta de ChatGPT.", "Codex ist nicht bei einem ChatGPT-Konto angemeldet.", "Aucun compte ChatGPT n’est connecté à Codex."}},
            {"errorApiKey", new[] {"The current account uses an API key; this panel shows ChatGPT plan limits.", "当前使用 API Key；此面板显示 ChatGPT 套餐额度。", "目前使用 API Key；此面板顯示 ChatGPT 方案額度。", "現在は API キーを使用中です。このパネルは ChatGPT プランの上限を表示します。", "현재 API 키를 사용 중입니다. 이 패널은 ChatGPT 요금제 한도를 표시합니다.", "La cuenta usa una clave API; este panel muestra los límites del plan de ChatGPT.", "Das Konto verwendet einen API-Schlüssel. Dieses Fenster zeigt ChatGPT-Planlimits.", "Le compte utilise une clé API ; ce panneau affiche les limites du forfait ChatGPT."}},
            {"errorNoLimits", new[] {"Codex did not return any displayable limits.", "Codex 暂未返回可显示的额度。", "Codex 暫未回傳可顯示的額度。", "Codex から表示可能な利用枠が返されませんでした。", "Codex가 표시할 한도를 반환하지 않았습니다.", "Codex no devolvió límites para mostrar.", "Codex hat keine anzeigbaren Kontingente zurückgegeben.", "Codex n’a renvoyé aucune limite à afficher."}},
            {"errorServer", new[] {"Codex service returned an error", "Codex 服务返回错误", "Codex 服務傳回錯誤", "Codex サービスからエラーが返されました", "Codex 서비스에서 오류를 반환했습니다", "El servicio de Codex devolvió un error", "Der Codex-Dienst hat einen Fehler zurückgegeben", "Le service Codex a renvoyé une erreur"}},
            {"errorTimeout", new[] {"Codex timed out. Try again shortly.", "Codex 读取超时，请稍后重试。", "Codex 讀取逾時，請稍後再試。", "Codex の応答がタイムアウトしました。しばらくしてから再試行してください。", "Codex 응답 시간이 초과되었습니다. 잠시 후 다시 시도하세요.", "Se agotó el tiempo de espera de Codex. Inténtalo de nuevo.", "Zeitüberschreitung bei Codex. Versuche es gleich noch einmal.", "Délai d’attente dépassé pour Codex. Réessayez bientôt."}},
            {"errorGeneric", new[] {"Could not read local Codex usage.", "读取本机 Codex 额度时出错。", "讀取本機 Codex 額度時發生錯誤。", "ローカルの Codex 使用量を取得できませんでした。", "로컬 Codex 사용량을 읽지 못했습니다.", "No se pudo leer el uso de Codex local.", "Die lokale Codex-Nutzung konnte nicht gelesen werden.", "Impossible de lire l’utilisation de Codex local."}}
        };

        private static readonly string PreferencePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CodexUsageBar", "language.txt"); // Preserve the existing language preference location.
        private static string preference = LoadPreference();

        public static string Preference { get { return preference; } }

        public static string Language
        {
            get
            {
                if (preference != "system") return preference;
                string culture = CultureInfo.CurrentUICulture.Name;
                if (culture.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                    culture.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase) ||
                    culture.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                    culture.StartsWith("zh-MO", StringComparison.OrdinalIgnoreCase)) return "zh-TW";
                if (culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
                foreach (string code in LanguageCodes)
                    if (culture.StartsWith(code, StringComparison.OrdinalIgnoreCase)) return code;
                return "en";
            }
        }

        public static string FontName
        {
            get
            {
                if (Language == "zh-CN") return "Microsoft YaHei UI";
                if (Language == "zh-TW") return "Microsoft JhengHei UI";
                if (Language == "ja") return "Yu Gothic UI";
                if (Language == "ko") return "Malgun Gothic";
                return "Segoe UI";
            }
        }

        public static void Select(string code)
        {
            if (!IsValid(code)) return;
            preference = code;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath));
                File.WriteAllText(PreferencePath, code, Encoding.UTF8);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public static string Get(string key)
        {
            string[] values;
            if (!Strings.TryGetValue(key, out values)) return key;
            for (int i = 0; i < LanguageCodes.Length; i++)
                if (LanguageCodes[i] == Language) return values[i];
            return values[0];
        }

        public static string LanguageName(string code)
        {
            switch (code)
            {
                case "en": return "English";
                case "zh-CN": return "简体中文";
                case "zh-TW": return "繁體中文";
                case "ja": return "日本語";
                case "ko": return "한국어";
                case "es": return "Español";
                case "de": return "Deutsch";
                case "fr": return "Français";
                default: return code;
            }
        }

        public static string Format(string key, params object[] values)
        {
            return String.Format(CultureInfo.InvariantCulture, Get(key), values);
        }

        public static string Window(int minutes)
        {
            int value;
            string unit;
            if (minutes % 1440 == 0) { value = minutes / 1440; unit = "day"; }
            else if (minutes % 60 == 0) { value = minutes / 60; unit = "hour"; }
            else { value = minutes; unit = "minute"; }
            if (Language == "zh-CN") return value + (unit == "day" ? " 天" : unit == "hour" ? " 小时" : " 分钟");
            if (Language == "zh-TW") return value + (unit == "day" ? " 天" : unit == "hour" ? " 小時" : " 分鐘");
            if (Language == "ja") return value + (unit == "day" ? "日" : unit == "hour" ? "時間" : "分");
            if (Language == "ko") return value + (unit == "day" ? "일" : unit == "hour" ? "시간" : "분");
            return value + " " + unit + (value == 1 ? "" : "s");
        }

        public static string WindowLabel(int minutes)
        {
            int value = minutes % 1440 == 0 ? minutes / 1440 : minutes % 60 == 0 ? minutes / 60 : minutes;
            if (Language == "zh-CN") return Window(minutes) + "额度";
            if (Language == "zh-TW") return Window(minutes) + "額度";
            if (Language == "ja") return Window(minutes) + "枠";
            if (Language == "ko") return Window(minutes) + " 한도";
            if (Language == "es") return "Límite de " + value + (minutes % 1440 == 0 ? " días" : minutes % 60 == 0 ? " horas" : " minutos");
            if (Language == "de") return value + (minutes % 1440 == 0 ? "-Tage-Limit" : minutes % 60 == 0 ? "-Stunden-Limit" : "-Minuten-Limit");
            if (Language == "fr") return "Limite de " + value + (minutes % 1440 == 0 ? " jours" : minutes % 60 == 0 ? " h" : " min");
            return value + (minutes % 1440 == 0 ? "-day limit" : minutes % 60 == 0 ? "-hour limit" : "-minute limit");
        }

        public static string ResetTime(long unixSeconds)
        {
            DateTime reset = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixSeconds).ToLocalTime();
            if (reset.Date == DateTime.Now.Date)
            {
                string today = Language == "zh-CN" || Language == "zh-TW" ? "今天 " :
                    Language == "ja" ? "今日 " : Language == "ko" ? "오늘 " :
                    Language == "es" ? "Hoy " : Language == "de" ? "Heute " :
                    Language == "fr" ? "Aujourd’hui " : "Today ";
                return today + reset.ToString("HH:mm");
            }
            if (Language == "zh-CN") return reset.ToString("MM月dd日 HH:mm");
            if (Language == "zh-TW" || Language == "ja") return reset.ToString("M月d日 HH:mm");
            if (Language == "ko") return reset.ToString("M월 d일 HH:mm");
            if (Language == "de") return reset.ToString("dd.MM. HH:mm");
            if (Language == "fr") return reset.ToString("d MMM HH:mm", CultureInfo.GetCultureInfo("fr-FR"));
            if (Language == "es") return reset.ToString("d MMM, HH:mm", CultureInfo.GetCultureInfo("es-ES"));
            return reset.ToString("MMM d, HH:mm", CultureInfo.GetCultureInfo("en-US"));
        }

        public static string ResetCredits(int count)
        {
            if (Language == "zh-CN") return "可用重置 " + count + " 次";
            if (Language == "zh-TW") return "可用重設 " + count + " 次";
            if (Language == "ja") return "リセット可能 " + count + " 回";
            if (Language == "ko") return "재설정 가능 " + count + "회";
            if (Language == "es") return "Reinicios disponibles: " + count;
            if (Language == "de") return "Zurücksetzungen verfügbar: " + count;
            if (Language == "fr") return "Réinitialisations disponibles : " + count;
            return count + (count == 1 ? " reset available" : " resets available");
        }

        public static string Error(UsageReadException error)
        {
            string message = Get(error.Key);
            return String.IsNullOrEmpty(error.Detail) ? message : message + ": " + error.Detail;
        }

        private static string LoadPreference()
        {
            try
            {
                if (File.Exists(PreferencePath))
                {
                    string code = File.ReadAllText(PreferencePath, Encoding.UTF8).Trim();
                    if (code == "zh") return "zh-CN";
                    if (IsValid(code)) return code;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return "system";
        }

        private static bool IsValid(string code)
        {
            if (code == "system") return true;
            foreach (string supported in LanguageCodes) if (supported == code) return true;
            return false;
        }
    }
}
