<?php
require "db.php";
require __DIR__ . "/includes/doctor_booking.php";

$sent = false;
$message = "";

$services = mysqli_query($conn, "SELECT id, name, price FROM services ORDER BY name");

$doctors = mysqli_query($conn, "SELECT id, name FROM admins WHERE is_active = 1 ORDER BY name, id");
$availableSlots = [];
if ($_POST) {
    $name = mysqli_real_escape_string($conn, $_POST['name']);
    $phone = mysqli_real_escape_string($conn, $_POST['phone']);
    $petName = mysqli_real_escape_string($conn, $_POST['pet_name']);
    $petType = mysqli_real_escape_string($conn, $_POST['pet_type']);
    $petAge = mysqli_real_escape_string($conn, $_POST['pet_age']);
    $client_comment = mysqli_real_escape_string($conn, $_POST['client_comment']);
    $serviceId = (int)$_POST['service_id'];
$doctorId = (int)($_POST["doctor_id"] ?? 0);
    $appointmentAt = trim($_POST['appointment_at']);
    $time = strtotime($appointmentAt);

    if ($serviceId <= 0 || !bookingTimeIsValid($appointmentAt) || !bookingDoctorExists($conn, $doctorId)) {
        $message = "Выберите услугу и свободное время";
    } else {
        $date = date("Y-m-d", $time);
        $clock = date("H:i:s", $time);
        $busyCheck = mysqli_query($conn, "SELECT id
                                         FROM zapic_na_priem
                                         WHERE data = '$date'
                                           AND time = '$clock' AND (doctor_id = $doctorId OR doctor_id IS NULL)
                                           AND status IN ('new', 'accepted', 'done')
                                         LIMIT 1");

        if (mysqli_num_rows($busyCheck) > 0) {
            $message = "Это время уже занято";
        } else {
            mysqli_query($conn, "INSERT INTO vladelci (name, phone)
                                 VALUES ('$name', '$phone')
                                 ON DUPLICATE KEY UPDATE name = VALUES(name)");

            $ownerResult = mysqli_query($conn, "SELECT id FROM vladelci WHERE phone = '$phone'");
            $owner = mysqli_fetch_assoc($ownerResult);
            $ownerId = (int)$owner['id'];

            mysqli_query($conn, "INSERT INTO pets (client_id, type, name, age)
                                 VALUES ($ownerId, '$petType', '$petName', '$petAge')
                                 ON DUPLICATE KEY UPDATE age = VALUES(age)");

            $petResult = mysqli_query($conn, "SELECT id FROM pets
                                                 WHERE client_id = $ownerId
                                                   AND type = '$petType'
                                                   AND name = '$petName'");
            $pet = mysqli_fetch_assoc($petResult);
            $petId = (int)$pet['id'];

            try {
                $appointmentCreated = mysqli_query($conn, "INSERT INTO zapic_na_priem (pet_id, service_id, doctor_id, data, time, status, client_comment)
                                                           VALUES ($petId, $serviceId, $doctorId, '$date', '$clock', 'new', '$client_comment')");

                if ($appointmentCreated) {
                    $sent = true;
                    $message = "Заявка отправлена";
                } elseif (mysqli_errno($conn) === 1062) {
                    $message = "Это время уже занято";
                } else {
                    error_log("Appointment creation failed: " . mysqli_error($conn));
                    $message = "Не удалось создать запись";
                }
            } catch (mysqli_sql_exception $e) {
                if ((int)$e->getCode() === 1062) {
                    $message = "Это время уже занято";
                } else {
                    error_log("Appointment creation failed: " . $e->getMessage());
                    $message = "Не удалось создать запись";
                }
            }
        }
    }
}
?>



<!doctype html>
<html lang="ru">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link
      href="https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&family=Roboto:ital,wght@0,100..900;1,100..900&display=swap"
      rel="stylesheet"
    />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link
      href="https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&family=Montserrat:ital,wght@0,100..900;1,100..900&family=Roboto:ital,wght@0,100..900;1,100..900&display=swap"
      rel="stylesheet"
    />
    <link rel="stylesheet" href="/veterenar/css/normalize.css" />
    <link rel="stylesheet" href="/veterenar/css/style.css" />
    <title>Ветеринарный кабинет</title>
  </head>

  <body>
    <div class="wrapper">
    <?php include "includes/header.php"; ?>
      <main class="main">
        <section class="headline">
          <div class="container">
            <div class="headline_inner">
              <div class="headline_text">
                <h1 class="headline_title">Ветеринарный кабинет</h1>
                <h2 class="headline_title_h2">
                  Здоровье вашего питомца — наша главная ценность
                </h2>
                <a href="#zapis_na_priem" class="headline-btn"
                  >Записаться на прием</a
                >
              </div>
              <img
                src="/veterenar/images/444444.png"
                alt=""
                class="headline_img"
              />
            </div>
          </div>
        </section>
        <section class="info">
          <div class="container">
            <div class="info__inner">
              <div class="info__text">
                <h2 class="info__title">Направления лечения</h2>
                <div class="categoria-grid">
                  <a
                    href="price.php?category=terapia"
                    class="grid-item terapia"
                  >
                    Терапевтические услуги
                  </a>

                  <a href="price.php?category=lab" class="grid-item lab">
                    Лабораторные исследования
                  </a>

                  <a
                    href="price.php?category=hirurgia"
                    class="grid-item hirurgia"
                  >
                    Хирургия
                  </a>

                  <a
                    href="price.php?category=vactinatia"
                    class="grid-item vactinatia"
                  >
                    Вакцинация и чипирование
                  </a>

                  <a
                    href="price.php?category=stomatologia"
                    class="grid-item stomatologia"
                  >
                    Стоматология
                  </a>

                  <a
                    href="price.php?category=allergia"
                    class="grid-item allergia"
                  >
                    Аллергология
                  </a>

                  <a href="price.php?category=other" class="grid-item other">
                    Прочие услуги
                  </a>
                </div>
              </div>
            </div>
          </div>
        </section>
        <section class="photo_info_bloc">
          <div class="container">
            <div class="photo_info_inner">
              <ul class="photo_info_list">
                <li class="photo_info_item">
                  <img
                    src="/veterenar/images/image1.jpg"
                    alt=""
                    class="photo_info_img"
                  />
                  <p>г.Бендеры</p>
                  <p>ул.Коммунальная, д.188</p>
                </li>
                <li class="photo_info_item">
                  <img
                    src="/veterenar/images/image2.jpg"
                    alt=""
                    class="photo_info_img"
                  />
                  <p>г.Тирасполь</p>
                  <p>ул.Советская, д.52</p>
                </li>
                <li class="photo_info_item">
                  <img
                    src="/veterenar/images/image3.jpg"
                    alt=""
                    class="photo_info_img"
                  />
                  <p>г.Григориополь</p>
                  <p>ул.Ленина, д.10</p>
                </li>
              </ul>
            </div>
          </div>
        </section>
        <section class="form_info">
          <div class="container">
            <div class="form_info_inner">
              <div class="form_wrapper">
                <h2 class="form_text" id="zapis_na_priem">Записаться на прием</h2>
                <?php if ($message != "") { ?>
                  <p class="<?php echo $sent ? 'form_success' : 'form_error'; ?>">
                    <?php echo $message; ?>
                  </p>
                <?php } ?>
                <form method="post" class="form">
                  <input
                    type="text"
                    name="name"
                    placeholder="Имя"
                    class="form__input"
                    required
                  />
                  <input
                    type="tel"
                    name="phone"
                    placeholder="Номер телефона"
                    class="form__input"
                    required
                  />
                  <input
                    type="text"
                    name="pet_name"
                    placeholder="Имя питомца"
                    class="form__input"
                    required
                  />
                  <input
                    type="text"
                    name="pet_type"
                    placeholder="Вид питомца"
                    class="form__input"
                    required
                  />
                  <input
                    type="text"
                    name="pet_age"
                    placeholder="Возраст питомца"
                    class="form__input"
                  />
                  <select name="service_id" class="form__input" required>
                    <option value="">Выберите услугу</option>
                    <?php mysqli_data_seek($services, 0); ?>
                    <?php while ($service = mysqli_fetch_assoc($services)) { ?>
                      <option value="<?php echo $service['id']; ?>">
                        <?php echo $service['name']; ?> — <?php echo $service['price']; ?> руб.
                      </option>
                    <?php } ?>
                  </select>
                  <label for="doctor_id">Врач</label>
                  <select id="doctor_id" name="doctor_id" class="form__input" required>
                    <option value="">Выберите врача</option>
                    <?php while ($doctor = mysqli_fetch_assoc($doctors)) { ?>
                      <option value="<?php echo (int)$doctor['id']; ?>">
                        <?php echo htmlspecialchars($doctor['name'], ENT_QUOTES, 'UTF-8'); ?>
                      </option>
                    <?php } ?>
                  </select>
                  <select id="appointment_at" name="appointment_at" class="form__input" required disabled>
                    <option value="">Выберите свободное время</option>
                    <?php foreach ($availableSlots as $slot) { ?>
                      <option value="<?php echo $slot['value']; ?>">
                        <?php echo $slot['label']; ?>
                      </option>
                    <?php } ?>
                  </select>
                  <textarea
                    name="client_comment"
                    placeholder="Комментарий"
                    class="form__input_comment"
                    required
                  ></textarea>
                  <button
                    type="submit"
                    class="form__btn"
                    <?php if ($sent) echo 'style="background:green;" '; ?>
                  >
                    <?php echo $sent ? "Заявка отправлена" : "Отправить"; ?>
                  </button>
                </form>
              </div>
              <div class="map">
                <iframe
                  src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d43669.671532122935!2d29.573687235722502!3d46.83671152674142!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x40c9032f7dc505f9%3A0x7cef9cecf870c541!2z0JLQtdGC0LXRgNC40L3QsNGA0L3Ri9C5INCy0YDQsNGH!5e0!3m2!1sru!2s!4v1769526432452!5m2!1sru!2s"
                  width="600"
                  height="450"
                  style="border: 0"
                  allowfullscreen=""
                  loading="lazy"
                  referrerpolicy="no-referrer-when-downgrade"
                ></iframe>
              </div>
            </div>
          </div>
        </section>
    </main>
      <?php include 'includes/footer.php'; ?>
    </div>
    <!-- подключаем Jquery -->
    <script src="scripts/jquery-4.0.0.js"></script>
    <script src="scripts/main.js"></script>
    <script src="scripts/doctor-booking.js"></script>
  </body>
</html>
