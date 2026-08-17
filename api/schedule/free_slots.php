<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: GET");

require "../../db.php";

$busy = [];
$startDate = date("Y-m-d");
$endDate = date("Y-m-d", strtotime("+4 days"));

$sql = "SELECT data, time
        FROM zapic_na_priem
        WHERE data BETWEEN ? AND ?
          AND status IN ('new', 'accepted', 'done')";
$stmt = $conn->prepare($sql);
$stmt->bind_param("ss", $startDate, $endDate);
$stmt->execute();
$result = $stmt->get_result();

while ($row = mysqli_fetch_assoc($result)) {
    $busy[$row["data"] . " " . $row["time"]] = true;
}

$days = [];
$now = time();

for ($d = 0; $d < 5; $d++) {
    $date = date("Y-m-d", strtotime("+$d days"));
    $slots = [];

    for ($hour = 8; $hour <= 18; $hour++) {
        foreach ([0, 30] as $minute) {
            if ($hour == 18 && $minute == 30) continue;

            $clock = sprintf("%02d:%02d:00", $hour, $minute);
            $slotTime = strtotime($date . " " . $clock);
            $key = $date . " " . $clock;

            $slots[] = [
                "time" => date("Y-m-d H:i:s", $slotTime),
                "label" => date("H:i", $slotTime),
                "is_available" => $slotTime > $now && !isset($busy[$key])
            ];
        }
    }

    $days[] = [
        "date" => $date,
        "label" => date("d.m.Y", strtotime($date)),
        "slots" => $slots
    ];
}

echo json_encode(["success" => true, "days" => $days], JSON_UNESCAPED_UNICODE);


