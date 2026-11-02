<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: GET");

require __DIR__ . "/../../db.php";
require __DIR__ . "/../../includes/visit_services.php";

$stats = [
    "requests_total" => 0,
    "requests_new" => 0,
    "requests_accepted" => 0,
    "requests_done" => 0,
    "requests_cancelled" => 0,
    "services_total" => 0
];

$sql = "SELECT status, COUNT(*) AS count
        FROM zapic_na_priem
        WHERE status IN ('new', 'accepted', 'done', 'cancelled')
        GROUP BY status";
$stmt = $conn->prepare($sql);
$stmt->execute();
$result = $stmt->get_result();

while ($row = mysqli_fetch_assoc($result)) {
    $status = $row["status"];
    $count = (int)$row["count"];

    $stats["requests_total"] += $count;

    if ($status == "new") $stats["requests_new"] = $count;
    if ($status == "accepted") $stats["requests_accepted"] = $count;
    if ($status == "done") $stats["requests_done"] = $count;
    if ($status == "cancelled") $stats["requests_cancelled"] = $count;
}

$sql = "SELECT COUNT(*) AS count FROM services";
$stmt = $conn->prepare($sql);
$stmt->execute();
$result = $stmt->get_result();
$row = mysqli_fetch_assoc($result);
$stats["services_total"] = (int)$row["count"];
if(visitServicesReady($conn)) {
$history=$conn->query('SELECT COALESCE(SUM(kol),0) AS quantity,COALESCE(SUM(total_price),0) AS cost,
    COUNT(DISTINCT priem_id)+COALESCE(SUM(priem_id IS NULL),0) AS visits FROM service_history')->fetch_assoc();
$stats['performed_services_total']=(int)$history['quantity'];
$stats['performed_services_cost']=(int)$history['cost'];
$stats['history_visits_total']=(int)$history['visits'];
}

echo json_encode(["success" => true, "stats" => $stats], JSON_UNESCAPED_UNICODE);


