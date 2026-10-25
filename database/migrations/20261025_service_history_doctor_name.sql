-- MariaDB (XAMPP). Run against the application's database.
-- Existing history rows stay unchanged; unknown historical doctors remain NULL.
ALTER TABLE service_history
    ADD COLUMN IF NOT EXISTS doctor_name VARCHAR(100) NULL AFTER service_name;

DELIMITER $$

CREATE OR REPLACE TRIGGER appointment_history_after_insert
AFTER INSERT ON zapic_na_priem FOR EACH ROW
BEGIN
    IF NEW.status = 'done' THEN
        INSERT INTO service_history
            (appointment_id, client_id, client_name, pet_id, pet_name,
             service_id, service_name, doctor_name, performed_at, quantity, total_price)
        SELECT NEW.id, v.id, v.name, p.id, p.name, s.id, s.name, doctor.name,
               CURRENT_TIMESTAMP, 1, s.price
        FROM pets p
        JOIN vladelci v ON v.id = p.client_id
        JOIN services s ON s.id = NEW.service_id
        LEFT JOIN admins doctor ON doctor.id = NEW.doctor_id
        WHERE p.id = NEW.pet_id;
        IF ROW_COUNT() <> 1 THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Cannot create completed appointment: service snapshot is missing';
        END IF;
    END IF;
END$$

CREATE OR REPLACE TRIGGER appointment_history_after_update
AFTER UPDATE ON zapic_na_priem FOR EACH ROW
BEGIN
    IF NEW.status = 'done' AND OLD.status <> 'done'
       AND NOT EXISTS (SELECT 1 FROM service_history WHERE appointment_id = NEW.id) THEN
        INSERT INTO service_history
            (appointment_id, client_id, client_name, pet_id, pet_name,
             service_id, service_name, doctor_name, performed_at, quantity, total_price)
        SELECT NEW.id, v.id, v.name, p.id, p.name, s.id, s.name, doctor.name,
               CURRENT_TIMESTAMP, 1, s.price
        FROM pets p
        JOIN vladelci v ON v.id = p.client_id
        JOIN services s ON s.id = NEW.service_id
        LEFT JOIN admins doctor ON doctor.id = NEW.doctor_id
        WHERE p.id = NEW.pet_id;
        IF ROW_COUNT() <> 1 THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Cannot complete appointment: service snapshot is missing';
        END IF;
    END IF;
END$$

DELIMITER ;
