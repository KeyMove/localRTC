<?php

/**
 * SimpleHttpServer - PHP 实现（带文件持久化）
 * 
 * 启动命令: php -S 0.0.0.0:8080 xl.php
 */

const MAX_ITEMS = 16;
const DATA_FILE = 'msg.json';

/**
 * 加载数据
 */
function loadData() {
    if (!file_exists(DATA_FILE)) {
        return [];
    }
    $content = file_get_contents(DATA_FILE);
    return json_decode($content, true) ?: [];
}

/**
 * 保存数据
 */
function saveData($data) {
    file_put_contents(DATA_FILE, json_encode($data, JSON_UNESCAPED_UNICODE), LOCK_EX);
}

/**
 * 处理请求
 */
function handleRequest() {
    header('Content-Type: application/json');
    header('Access-Control-Allow-Origin: *');
    
    $info = $_GET['info'] ?? null;
    
    if (!empty($info)) {
        $dataStore = loadData();
        $dataStore[] = $info;
        
        if (count($dataStore) > MAX_ITEMS) {
            array_shift($dataStore);
        }
        
        saveData($dataStore);
        echo json_encode(['status' => 'ok']);
    } else {
        $dataStore = loadData();
        // 手动拼接 JSON，避免对存储的 JSON 字符串二次转义
        $items = [];
        foreach ($dataStore as $item) {
            $items[] = $item;
        }
        
        echo '[' . implode(',', $items) . ']';
    }
}

try {
    handleRequest();
} catch (Exception $e) {
    http_response_code(500);
    echo json_encode(['error' => $e->getMessage()]);
}