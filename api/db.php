<?php

// Подключение к базе сайта.
$conn = mysqli_connect("localhost", "root", "", "vet_clinic");

if (!$conn) {
  die("Ошибка подключения: " . mysqli_connect_error());
}
mysqli_set_charset($conn, "utf8mb4"); // для нормального отображения кириллицы
?>

