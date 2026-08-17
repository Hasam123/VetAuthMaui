<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: GET");

require "../../db.php";

$services = [];

$sql = "SELECT id, name, description, price, category
                               FROM services
                               ORDER BY name";
$stmt = $conn->prepare($sql);
$stmt->execute();
$result = $stmt->get_result();

while ($row = mysqli_fetch_assoc($result)) {
    $services[] = [
        "id" => (int)$row["id"],
        "title" => $row["name"],
        "description" => $row["description"],
        "price" => (int)$row["price"],
        "category" => $row["category"]
    ];
}

echo json_encode(["success" => true, "services" => $services], JSON_UNESCAPED_UNICODE);


