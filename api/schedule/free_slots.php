<?php
header("Content-Type: application/json; charset=utf-8");
header("Access-Control-Allow-Origin: *");
header("Access-Control-Allow-Methods: GET");

require __DIR__ . "/../../db.php";
require __DIR__ . "/../../includes/doctor_booking.php";
$doctorId = (int)($_GET["doctor_id"] ?? 0);
if (!bookingDoctorExists($conn, $doctorId)) {
    echo json_encode(["success" => false, "message" => "Выберите врача", "days" => []], JSON_UNESCAPED_UNICODE);
    exit;
}

$doctorIsActive = bookingDoctorIsActive($conn, $doctorId);
$busy = [];
$now = time();
$startDate = date("Y-m-d", $now);
$endDate = date("Y-m-d", strtotime("+4 days", $now));

$sql = "SELECT data, time
        FROM zapic_na_priem
        WHERE data BETWEEN ? AND ? AND (doctor_id = ? OR doctor_id IS NULL)
          AND status IN ('new', 'accepted', 'done')";
$stmt = $conn->prepare($sql);
$stmt->bind_param("ssi", $startDate, $endDate, $doctorId);
$stmt->execute();
$result = $stmt->get_result();

while ($row = mysqli_fetch_assoc($result)) {
    $busy[$row["data"] . " " . $row["time"]] = true;
}

$days = [];

for ($d = 0; $d < 5; $d++) {
    $date = date("Y-m-d", strtotime("+$d days", $now));
    $slots = [];

    for ($hour = 8; $hour <= 18; $hour++) {
        foreach ([0, 30] as $minute) {
            if ($hour == 18 && $minute == 30) continue;

            $clock = sprintf("%02d:%02d:00", $hour, $minute);
            $slotTime = strtotime($date . " " . $clock);
            $key = $date . " " . $clock;

            $slots[] = [
                "time" => date("Y-m-d H:i:s", $slotTime),
                "starts_at" => $slotTime,
                "label" => date("H:i", $slotTime),
                "is_available" => $doctorIsActive && $slotTime > $now && !isset($busy[$key])
            ];
        }
    }

    $days[] = [
        "date" => $date,
        "label" => date("d.m.Y", strtotime($date)),
        "slots" => $slots
    ];
}

echo json_encode(["success" => true, "days" => $days, "server_now" => $now,
    "timezone" => date_default_timezone_get()], JSON_UNESCAPED_UNICODE);


