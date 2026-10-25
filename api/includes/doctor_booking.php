<?php
// Shared validation for website and mobile API bookings.
function bookingDoctorExists(mysqli $conn, int $doctorId): bool
{
    $stmt = $conn->prepare("SELECT id FROM admins WHERE id = ?");
    $stmt->bind_param("i", $doctorId);
    $stmt->execute();
    return $stmt->get_result()->num_rows === 1;
}

function bookingTimeIsValid(string $value): bool
{
    $date = DateTime::createFromFormat("!Y-m-d H:i:s", $value);
    if (!$date || $date->format("Y-m-d H:i:s") !== $value) {
        return false;
    }
    $clock = $date->format("H:i:s");
    return $date->getTimestamp() > time()
        && $date->format("Y-m-d") <= date("Y-m-d", strtotime("+4 days"))
        && $clock >= "08:00:00" && $clock <= "18:00:00"
        && in_array($date->format("i:s"), ["00:00", "30:00"], true);
}
