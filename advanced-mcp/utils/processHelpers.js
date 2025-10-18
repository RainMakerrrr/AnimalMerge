/**
 * 🔥 PROCESS HELPERS - УНИВЕРСАЛЬНЫЕ ОБЁРТКИ ДЛЯ CHILD_PROCESS
 * 
 * 💡 АРХИТЕКТУРНАЯ ИДЕЯ: Все spawn/exec операции должны быть обёрнуты в промисы
 * чтобы ошибки попадали в дерево вызовов, а не в unhandled events!
 * 
 * 🎯 ПРИНЦИПЫ:
 * - Все асинхронные операции возвращают промисы
 * - Error events превращаются в rejected promises
 * - Никаких unhandled errors!
 */

import { spawn, exec } from 'child_process';
import { promisify } from 'util';
import fs from 'fs';
import path from 'path';

// 🔧 ПРОМИСИФИЦИРОВАННЫЙ EXEC
export const execAsync = promisify(exec);

/**
 * 🚀 БЕЗОПАСНЫЙ SPAWN - ОБЁРТКА В ПРОМИС
 * 
 * @param {string} command - Команда для выполнения
 * @param {string[]} args - Аргументы команды
 * @param {object} options - Опции spawn
 * @returns {Promise<ChildProcess>} Промис с процессом
 */
export function spawnAsync(command, args = [], options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      shell: true,           // 🔥 ВСЕГДА SHELL TRUE!
      env: process.env,      // Наследуем все переменные окружения
      ...options
    });

    // Обработка успешного завершения
    child.on('spawn', () => {
      resolve(child);
    });

    // Обработка ошибок запуска
    child.on('error', (error) => {
      reject(new Error(`Failed to spawn ${command}: ${error.message}`));
    });

    // Обработка неожиданного завершения
    child.on('exit', (code, signal) => {
      if (code !== 0 && code !== null) {
        reject(new Error(`Process ${command} exited with code ${code}`));
      }
    });
  });
}

/**
 * 🎯 SPAWN С ОЖИДАНИЕМ ЗАВЕРШЕНИЯ
 * 
 * @param {string} command - Команда для выполнения
 * @param {string[]} args - Аргументы команды
 * @param {object} options - Опции spawn
 * @returns {Promise<{stdout: string, stderr: string, code: number}>}
 */
export function spawnWithOutput(command, args = [], options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      shell: true,
      env: process.env,
      ...options
    });

    let stdout = '';
    let stderr = '';

    if (child.stdout) {
      child.stdout.on('data', (data) => {
        stdout += data.toString();
      });
    }

    if (child.stderr) {
      child.stderr.on('data', (data) => {
        stderr += data.toString();
      });
    }

    child.on('error', (error) => {
      reject(new Error(`Failed to spawn ${command}: ${error.message}`));
    });

    child.on('close', (code) => {
      resolve({
        stdout: stdout.trim(),
        stderr: stderr.trim(),
        code: code || 0
      });
    });
  });
}

/**
 * 🔥 SPAWN В ФОНЕ С ПРОВЕРКОЙ ЗАПУСКА
 * 
 * @param {string} command - Команда для выполнения
 * @param {string[]} args - Аргументы команды
 * @param {object} options - Опции spawn
 * @param {function} healthCheck - Функция проверки что процесс запустился
 * @returns {Promise<ChildProcess>} Промис с запущенным процессом
 */
export function spawnBackground(command, args = [], options = {}, healthCheck = null) {
  return new Promise(async (resolve, reject) => {
    try {
      const process = await spawnAsync(command, args, {
        detached: false,  // Не отсоединяем от родительского процесса
        stdio: 'pipe',    // Перехватываем вывод
        ...options
      });

      process.unref(); // Позволяет процессу работать независимо

      // 🔍 ЕСЛИ ЕСТЬ HEALTH CHECK - ЖДЁМ ПОКА ПРОЦЕСС ЗАПУСТИТСЯ
      if (healthCheck && typeof healthCheck === 'function') {
        try {
          await healthCheck();
          resolve(process);
        } catch (error) {
          reject(new Error(`Health check failed: ${error.message}`));
        }
      } else {
        resolve(process);
      }

    } catch (error) {
      reject(error);
    }
  });
}

/**
 * 🎯 ПРОВЕРКА СУЩЕСТВОВАНИЯ КОМАНДЫ
 * 
 * @param {string} command - Команда для проверки
 * @returns {Promise<boolean>} true если команда существует
 */
export async function commandExists(command) {
  try {
    const isWindows = process.platform === 'win32';
    const checkCmd = isWindows ? 'where' : 'which';
    const result = await spawnWithOutput(checkCmd, [command]);
    return result.code === 0 && result.stdout.length > 0;
  } catch (error) {
    return false;
  }
}

/**
 * 🛡️ БЕЗОПАСНЫЙ ЗАПУСК С ПРОВЕРКОЙ СУЩЕСТВОВАНИЯ КОМАНДЫ
 * 
 * @param {string} command - Команда для выполнения
 * @param {string[]} args - Аргументы команды
 * @param {object} options - Опции spawn
 * @returns {Promise<ChildProcess>} Промис с процессом
 */
export async function safeSpawn(command, args = [], options = {}) {
  // 🔍 ПРОВЕРЯЕМ ЧТО КОМАНДА СУЩЕСТВУЕТ
  const exists = await commandExists(command);
  if (!exists) {
    throw new Error(`Command "${command}" not found in PATH`);
  }

  return spawnAsync(command, args, options);
}

// 🔥 execAsync уже экспортирован в строке 18 