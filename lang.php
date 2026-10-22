<?php
// Язык страниц ShotHost: английский по умолчанию, русский по адресу /shothost/ru/.
// Тексты лежат в lang/en.php и lang/ru.php, ключи в них одинаковые.

// Отрезает префикс ru/ от пути внутри /shothost/ и возвращает язык
function shothost_lang(string &$relative_path): string
{
    $path = ltrim($relative_path, '/');
    if ($path === 'ru' || strncmp($path, 'ru/', 3) === 0) {
        $relative_path = (string) substr($path, 2);
        return 'ru';
    }
    return 'en';
}

// Текст по ключу на текущем языке; если перевода нет, берётся английский
function t(string $key): string
{
    global $shothost_texts, $shothost_texts_en;
    return $shothost_texts[$key] ?? $shothost_texts_en[$key] ?? $key;
}

function shothost_load_texts(string $lang): void
{
    global $shothost_texts, $shothost_texts_en;
    $shothost_texts_en = require __DIR__ . '/lang/en.php';
    $shothost_texts = $lang === 'en' ? $shothost_texts_en : require __DIR__ . '/lang/' . $lang . '.php';
}
