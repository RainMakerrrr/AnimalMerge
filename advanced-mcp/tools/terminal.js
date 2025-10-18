/**
 * 💻 TERMINAL TOOLS - Виртуальный MCP сервер для системы
 * 
 * 🚀 Все команды для работы с системой в одном модуле!
 * Системная информация, проверка портов, процессы - всё здесь!
 * 
 * 🤯 НОВАЯ ФИЧА: АВТОМАТИЧЕСКАЯ SYSTEM INFO В КАЖДОМ ОТВЕТЕ!
 * 🖥️ НОВАЯ ФИЧА: АВТОМАТИЧЕСКИЕ СИСТЕМНЫЕ СКРИНШОТЫ (через глобальный декоратор)!
 */

import path from 'path';
import fs from 'fs/promises';
// Используем нативный fetch из Node.js 18+
// 🔥 ИСПОЛЬЗУЕМ БЕЗОПАСНЫЕ ОБЁРТКИ ИЗ PROCESS HELPERS!
import { execAsync, spawnAsync, spawnWithOutput, spawnBackground } from '../utils/processHelpers.js';
import { logInfo, logError, extractErrorDetails } from '../utils/logger.js';
import { getWorkspaceRoot, resolveWorkspacePath } from '../utils/workspaceUtils.js';

// 💻 ЭКСПОРТ ВСЕХ TERMINAL КОМАНД
export const terminalTools = [
  {
    name: "echo",
    description: "🔥 ЭХОЛОКАТОР! Твой терминальный попугай повторяет слова! 🔥\n\n" +
      "🗣️ ГОВОРИТ ТЕБЕ: 'Скажи что-нибудь - я повторю и покажу что всё работает!'\n" +
      "📊 ДАЕТ ДАННЫЕ: Твое сообщение в красивом формате с подтверждением\n" +
      "💡 НАПРАВЛЯЕТ: Используй для тестирования связи с терминальными инструментами\n" +
      "🐕 ТВОЙ ТЕРМИНАЛЬНЫЙ ПОПУГАЙ: Простейший способ проверить что MCP работает",
    inputSchema: {
      type: "object",
      properties: {
        message: { type: "string", description: "Сообщение для повтора" }
      },
      required: ["message"]
    },
    handler: async (args, { log, logInfo, logError, logSuccess }) => {
      const { message } = args;

      // 🧪 ТЕСТИРУЕМ ЛОГИРОВАНИЕ!
      logInfo(`🧪 ТЕСТ: Получено сообщение для эха: ${message}`);
      logSuccess(`✅ ТЕСТ: Эхо команда выполняется успешно`);
      logError(`🔴 ТЕСТ: Это тестовая ошибка для проверки логов`);

      // 🤖 ПРОСТО ВОЗВРАЩАЕМ ТЕКСТ - mcpServer автоматически обернёт в правильный формат!
      return `🔥 **ECHO FROM TERMINAL TOOLS** 🔥\n\n` +
        `📢 **Message:** ${message}\n\n` +
        `✅ Эхо работает! Это Terminal Tools в действии!`;
    }
  },

  {
    name: "system_info",
    description: "📊 СИСТЕМНЫЙ ДИАГНОСТ! Твой цифровой доктор проверяет здоровье системы! 📊\n\n" +
      "🗣️ ГОВОРИТ ТЕБЕ: 'Покажи мне систему - расскажу всё о портах, процессах, времени!'\n" +
      "📊 ДАЕТ ДАННЫЕ: Время MSK, статус портов, количество Node.js процессов\n" +
      "💡 НАПРАВЛЯЕТ: include_processes=true покажет детали всех Node.js процессов\n" +
      "🐕 ТВОЙ СИСТЕМНЫЙ ДОКТОР: Мониторит порты 1337, 3000, 3001, 8080, 5000",
    inputSchema: {
      type: "object",
      properties: {
        include_processes: { type: "boolean", default: false, description: "Включить список процессов" },
        max_processes: { type: "number", default: 10, description: "Максимум процессов для показа" }
      },
      required: []
    },
    handler: async (args) => {
      const { include_processes = false, max_processes = 10 } = args;

      try {
        // Время в MSK
        const now = new Date();
        const mskTime = new Intl.DateTimeFormat('ru-RU', {
          timeZone: 'Europe/Moscow',
          year: 'numeric',
          month: '2-digit',
          day: '2-digit',
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit'
        }).format(now);

        // Проверка портов (кроссплатформенно)
        const isWindows = process.platform === 'win32';
        const checkPort = async (port) => {
          try {
            if (isWindows) {
              const { stdout } = await execAsync(`netstat -ano | findstr :${port}`);
              return stdout.trim() ? '🟢 ACTIVE' : '🔴 CLOSED';
            } else {
              const { stdout } = await execAsync(`lsof -i :${port}`);
              return stdout.trim() ? '🟢 ACTIVE' : '🔴 CLOSED';
            }
          } catch {
            return '🔴 CLOSED';
          }
        };

        const ports = {
          1337: await checkPort(1337),
          3000: await checkPort(3000),
          3001: await checkPort(3001), // 🔥 ДОБАВИЛ ПОРТ 3001 ДЛЯ VS CODE МОСТА!
          8080: await checkPort(8080),
          5000: await checkPort(5000)
        };

        // Процессы Node.js (кроссплатформенно)
        let nodeProcesses = 0;
        try {
          if (isWindows) {
            const { stdout } = await execAsync('tasklist | findstr /I node');
            nodeProcesses = stdout.split('\n').filter(line => line.trim()).length;
          } else {
            const { stdout } = await execAsync('pgrep -f node');
            nodeProcesses = stdout.split('\n').filter(line => line.trim()).length;
          }
        } catch {
          nodeProcesses = 0;
        }

        let systemInfo = `📊 **SYSTEM INFO FROM TERMINAL TOOLS** 📊\n\n` +
          `🕐 **Time (MSK):** ${mskTime}\n\n` +
          `🌐 **Port Status:**\n` +
          `  • 1337: ${ports[1337]}\n` +
          `  • 3000: ${ports[3000]}\n` +
          `  • 3001: ${ports[3001]} 🔥 VS Code Bridge\n` +
          `  • 8080: ${ports[8080]}\n` +
          `  • 5000: ${ports[5000]}\n\n` +
          `⚡ **Node.js Processes:** ${nodeProcesses}\n\n`;

        if (include_processes && nodeProcesses > 0) {
          try {
            const { stdout } = isWindows
              ? await execAsync('wmic process where "name like \'%node%\'" get ProcessId,WorkingSetSize,CommandLine /FORMAT:LIST')
              : await execAsync('ps aux | grep -i node | grep -v grep');
            const processes = stdout.split('\n')
              .filter(line => line.trim())
              .slice(0, max_processes)
              .map(line => {
                if (isWindows) {
                  const pidMatch = line.match(/ProcessId=(\d+)/i);
                  const wsMatch = line.match(/WorkingSetSize=(\d+)/i);
                  const pid = pidMatch ? pidMatch[1] : 'N/A';
                  const memMb = wsMatch ? Math.round(parseInt(wsMatch[1], 10) / 1024 / 1024) : 'N/A';
                  return `  • PID ${pid}: ${memMb}MB`;
                } else {
                  const parts = line.trim().split(/\s+/);
                  return `  • PID ${parts[1]}: ${Math.round(parseFloat(parts[5]) / 1024)}MB (${parts[3]}% CPU)`;
                }
              });

            systemInfo += `📋 **Node.js Processes:**\n${processes.join('\n')}\n\n`;
          } catch (error) {
            systemInfo += `❌ **Process List Error:** ${error.message}\n\n`;
          }
        }

        systemInfo += `💻 **Powered by Terminal Tools!**`;

        return systemInfo;
      } catch (error) {
        throw new Error(`❌ **SYSTEM INFO ERROR** ❌\n\nError: ${error.message}`);
      }
    }
  },

  {
    name: "check_port",
    description: "🔍 ПОРТОВЫЙ ИНСПЕКТОР! Твой сетевой детектив проверяет порты! 🔍\n\n" +
      "🗣️ ГОВОРИТ ТЕБЕ: 'Дай номер порта - скажу активен он или спит!'\n" +
      "📊 ДАЕТ ДАННЫЕ: Статус порта (ACTIVE/CLOSED) с деталями netstat\n" +
      "💡 НАПРАВЛЯЕТ: Используй для проверки запущенных серверов и сервисов\n" +
      "🐕 ТВОЙ СЕТЕВОЙ ДЕТЕКТИВ: Использует netstat для точной диагностики",
    inputSchema: {
      type: "object",
      properties: {
        port: { type: "number", description: "Номер порта для проверки" },
        protocol: { type: "string", enum: ["tcp", "udp"], default: "tcp", description: "Протокол для проверки" }
      },
      required: ["port"]
    },
    handler: async (args) => {
      const { port, protocol = "tcp" } = args;

      try {
        const isWindows = process.platform === 'win32';
        if (isWindows) {
          const { stdout } = await execAsync(`netstat -ano | findstr :${port}`);
          const isActive = stdout.trim() ? true : false;
          return `🔍 **PORT CHECK FROM TERMINAL TOOLS** 🔍\n\n` +
            `🌐 **Port:** ${port}\n` +
            `📡 **Protocol:** ${protocol.toUpperCase()}\n` +
            `📊 **Status:** ${isActive ? '🟢 ACTIVE' : '🔴 CLOSED'}\n\n` +
            (isActive ? `📝 **Details:**\n\`\`\`\n${stdout.trim()}\n\`\`\`` : '💤 Port is not in use') +
            `\n\n💻 **Checked by Terminal Tools!**`;
        }
        const { stdout } = await execAsync(`lsof -i :${port}`);
        const isActive = stdout.trim() ? true : false;

        return `🔍 **PORT CHECK FROM TERMINAL TOOLS** 🔍\n\n` +
          `🌐 **Port:** ${port}\n` +
          `📡 **Protocol:** ${protocol.toUpperCase()}\n` +
          `📊 **Status:** ${isActive ? '🟢 ACTIVE' : '🔴 CLOSED'}\n\n` +
          (isActive ? `📝 **Details:**\n\`\`\`\n${stdout.trim()}\n\`\`\`` : '💤 Port is not in use') +
          `\n\n💻 **Checked by Terminal Tools!**`;
      } catch (error) {
        throw new Error(`❌ **PORT CHECK ERROR** ❌\n\n` +
          `🌐 **Port:** ${port}\n` +
          `📡 **Protocol:** ${protocol.toUpperCase()}\n` +
          `💥 **Error:** ${error.message}`);
      }
    }
  },

  // 🔥 НОВЫЕ СТАБИЛЬНЫЕ ИНСТРУМЕНТЫ ДЛЯ MACOS!
  {
    name: "find_process",
    description: "🔍 ОХОТНИК ЗА ПРОЦЕССАМИ! Твой системный следопыт находит программы! 🔍\n\n" +
      "🗣️ ГОВОРИТ ТЕБЕ: 'Дай имя программы - найду все её процессы в системе!'\n" +
      "📊 ДАЕТ ДАННЫЕ: Список найденных процессов с PID и использованием памяти\n" +
      "💡 НАПРАВЛЯЕТ: Используй для поиска node, chrome, любых программ\n" +
      "🐕 ТВОЙ СИСТЕМНЫЙ СЛЕДОПЫТ: Использует ps aux для надежного поиска процессов",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Имя процесса для поиска" }
      },
      required: ["name"]
    },
    handler: async (args) => {
      const { name } = args;
      const isWindows = process.platform === 'win32';

      try {
        let stdout = '';
        if (isWindows) {
          const res = await execAsync(`tasklist | findstr /I "${name}"`);
          stdout = res.stdout;
        } else {
          const res = await execAsync(`ps aux | grep -i "${name}" | grep -v grep`);
          stdout = res.stdout;
        }
        const result = stdout.trim();

        if (result) {
          return `🔍 **PROCESS FOUND** 🔍\n\n` +
            `📋 **Search:** ${name}\n\n` +
            `📝 **Results:**\n\`\`\`\n${result}\n\`\`\`\n\n` +
            `💻 **Found by Terminal Tools!**`;
        } else {
          throw new Error(`❌ **PROCESS NOT FOUND** ❌\n\n` +
            `📋 **Search:** ${name}\n` +
            `💤 **Status:** No processes found with this name`);
        }
      } catch (error) {
        throw new Error(`❌ **PROCESS SEARCH ERROR** ❌\n\n` +
          `📋 **Search:** ${name}\n` +
          `💥 **Error:** ${error.message}`);
      }
    }
  },

  {
    name: "safe_curl",
    description: "🌐 ВЕБЛОКАТОР! Твой HTTP-курьер доставляет запросы без глюков! 🌐\n\n" +
      "🗣️ ГОВОРИТ ТЕБЕ: 'Дай URL - отправлю GET/POST/PUT/DELETE запрос надежно!'\n" +
      "📊 ДАЕТ ДАННЫЕ: Ответ сервера, статус код, заголовки, тело ответа\n" +
      "💡 НАПРАВЛЯЕТ: Используй для API тестирования без проблем curl\n" +
      "🐕 ТВОЙ HTTP КУРЬЕР: Стабильные запросы через нативный fetch Node.js",
    inputSchema: {
      type: "object",
      properties: {
        url: { type: "string", description: "URL для запроса" },
        method: { type: "string", enum: ["GET", "POST", "PUT", "DELETE"], default: "GET", description: "HTTP метод" },
        data: { type: "string", description: "Данные для POST/PUT запросов" }
      },
      required: ["url"]
    },
    handler: async (args) => {
      const { url, method = "GET", data } = args;

      try {
        const fetchOptions = { method, headers: {} };
        if (data) {
          fetchOptions.body = data;
          fetchOptions.headers['Content-Type'] = 'application/json';
        }

        const res = await fetch(url, fetchOptions);
        const text = await res.text();

        let response = `🌐 **HTTP REQUEST** 🌐\n\n` +
          `📡 **Method:** ${method}\n` +
          `🔗 **URL:** ${url}\n` +
          `📊 **Status:** ${res.status}\n`;

        if (data) {
          response += `📝 **Data:** ${data}\n`;
        }

        response += `\n📋 **Response:**\n\`\`\`\n${text}\n\`\`\``;
        response += `\n\n💻 **Powered by Terminal Tools!**`;

        return response;
      } catch (error) {
        throw new Error(`❌ **HTTP REQUEST ERROR** ❌\n\n` +
          `📡 **Method:** ${method}\n` +
          `🔗 **URL:** ${url}\n` +
          `💥 **Error:** ${error.message}`);
      }
    }
  },

  {
    name: "wait_for_user",
    description: "⏳ ИНТЕРАКТИВНЫЙ ПОМОЩНИК! Твой человеческий интерфейс для вопросов и уточнений! ⏳\n\n" +
      "🗣️ ГОВОРИТ ТЕБЕ: 'Нужно что-то спросить? Покажу вопрос пользователю и получу его ответ!'\n" +
      "📊 ДАЕТ ДАННЫЕ: Текстовый ответ пользователя на твой вопрос или подтверждение действия\n" +
      "💡 НАПРАВЛЯЕТ: expect_answer=true для получения текста, false для простого подтверждения\n" +
      "🐕 ТВОЙ ИНТЕРАКТИВНЫЙ ИНТЕРФЕЙС: Мост между ИИ и человеком для диалога и уточнений",
    inputSchema: {
      type: "object",
      properties: {
        request: { type: "string", description: "Вопрос или просьба к пользователю" },
        details: { type: "string", description: "Дополнительные детали (опционально)" },
        expect_answer: { 
          type: "boolean", 
          default: false, 
          description: "true = ожидать текстовый ответ, false = простое подтверждение" 
        },
        answer_placeholder: {
          type: "string",
          default: "Введите ваш ответ...",
          description: "Подсказка для поля ввода (только при expect_answer=true)"
        }
      },
      required: ["request"]
    },
    handler: async (args) => {
      const { 
        request, 
        details = '', 
        expect_answer = false,
        answer_placeholder = "Введите ваш ответ..."
      } = args;
      const os = process.platform;
      
      const title = expect_answer ? "❓ ВОПРОС ОТ ИИ ❓" : "⏳ ПРОСЬБА К ПОЛЬЗОВАТЕЛЮ ⏳";
      const fullRequest = details 
        ? `🎯 ${request}\n\n📝 Детали: ${details}`
        : `🎯 ${request}`;

      try {
        if (os === 'darwin') {
          if (expect_answer) {
            // macOS: диалог с полем ввода для получения ответа
            const script = `display dialog "${fullRequest.replace(/"/g, '\\"')}" with title "${title}" default answer "${answer_placeholder}" buttons {"Отправить", "Отмена"} default button "Отправить"`;
            try {
              const { stdout } = await execAsync(`osascript -e '${script}'`);
              // Извлекаем введенный текст из ответа osascript
              const match = stdout.match(/text returned:(.+)/);
              if (match) {
                const userAnswer = match[1].trim();
                return `💬 **ОТВЕТ ПОЛЬЗОВАТЕЛЯ:**\n\n"${userAnswer}"`;
              } else {
                throw new Error("❌ Не удалось получить ответ пользователя.");
              }
            } catch (error) {
              throw new Error("❌ Пользователь отменил ввод ответа.");
            }
          } else {
            // macOS: простое подтверждение (старая логика)
            const script = `display dialog "${fullRequest.replace(/"/g, '\\"')}" with title "${title}" buttons {"Выполнено", "Отмена"} default button "Выполнено"`;
            try {
              const { stdout } = await execAsync(`osascript -e '${script}'`);
              if (stdout.includes("Выполнено")) {
                return "✅ Пользователь подтвердил выполнение.";
              } else {
                throw new Error("❌ Пользователь отменил операцию.");
              }
            } catch (error) {
              throw new Error("❌ Пользователь отменил операцию.");
            }
          }
        } else {
          // Windows/Linux: используем старый метод с терминалом
          if (expect_answer) {
            // Для других ОС пока оставляем упрощенную версию
            const command = os === 'win32'
              ? `start cmd /k "echo ${title} && echo. && echo ${fullRequest} && echo. && echo 📝 Введите ваш ответ в чат Cursor && echo. && pause"`
              : `x-terminal-emulator -e "bash -c 'echo \\"${title}\\"; echo; echo \\"${fullRequest}\\"; echo; echo \\"📝 Введите ваш ответ в чат Cursor\\"; read -p \\"Нажмите Enter...\\"'"`
            
            await spawnBackground(command);
            return "❓ Пожалуйста, введите ваш ответ в следующем сообщении в чате.";
          } else {
            const command = os === 'win32' 
              ? `start cmd /k "echo ${title} && echo. && echo ${fullRequest} && echo. && echo ✅ Закрой этот терминал когда выполнишь && echo. && echo 🤝 Жду твоего действия... && echo. && pause"`
              : `x-terminal-emulator -e "bash -c 'echo \\"${title}\\"; echo; echo \\"${fullRequest}\\"; echo; read -p \\"Нажмите Enter, когда закончите...\\"'"`

            await spawnBackground(command);
            return "⏳ Ожидание пользователя... Пожалуйста, следуйте инструкциям в новом окне терминала.";
          }
        }
      } catch (error) {
        throw new Error(`❌ **ОШИБКА ИНТЕРАКТИВНОГО ИНТЕРФЕЙСА** ❌\n\n💥 ${error.message}`);
      }
    }
  }
];

export const terminalModule = {
  namespace: "terminal",
  description: "Системные инструменты",
  tools: terminalTools
};

/**
 * 💻 TERMINAL TOOLS - МОДУЛЬ ЗАВЕРШЁН!
 * 
 * ✅ Все системные команды в одном месте
 * ✅ Проверка портов и процессов
 * ✅ Чистый экспорт для импорта в index.js
 * ✅ 🤯 АВТОМАТИЧЕСКАЯ SYSTEM INFO В КАЖДОМ ОТВЕТЕ!
 * ✅ Готов к использованию!
 */



