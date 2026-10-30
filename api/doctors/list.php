<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
require __DIR__ . "/../../db.php";

$result = $conn->query("SELECT id, name FROM admins WHERE is_active = 1 ORDER BY name, id");
$doctors = [];
while ($row = $result->fetch_assoc()) {
    $doctors[] = ["id" => (int)$row["id"], "name" => $row["name"]];
}
echo json_encode(["success" => true, "doctors" => $doctors], JSON_UNESCAPED_UNICODE);
