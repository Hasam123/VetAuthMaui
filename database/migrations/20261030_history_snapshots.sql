-- Immutable copies of source values. Existing historical rows are not backfilled.
ALTER TABLE service_history
    ADD COLUMN IF NOT EXISTS address VARCHAR(255) NULL,
    ADD COLUMN IF NOT EXISTS pet_type VARCHAR(50) NULL,
    ADD COLUMN IF NOT EXISTS pol VARCHAR(10) NULL,
    ADD COLUMN IF NOT EXISTS pet_age VARCHAR(15) NULL,
    ADD COLUMN IF NOT EXISTS service_category VARCHAR(100) NULL,
    ADD COLUMN IF NOT EXISTS vid_priema VARCHAR(10) NULL;

DELIMITER $$
CREATE OR REPLACE TRIGGER appointment_history_after_insert
AFTER INSERT ON zapic_na_priem FOR EACH ROW
BEGIN
    IF NEW.status = 'done' THEN
        INSERT INTO service_history
            (client_id, client_name, pet_id, pet_name, service_name, doctor_name,
             data_created, kol, total_price, address, pet_type, pol, pet_age,
             service_category, vid_priema)
        SELECT v.id, v.name, p.id, p.name, s.name, d.name, CURRENT_TIMESTAMP, 1,
               s.price, v.address, p.type, p.pol, p.age, s.category,
               CASE WHEN NEW.perv_priem_id IS NULL THEN 'Первичный' ELSE 'Повторный' END
        FROM pets p JOIN vladelci v ON v.id=p.client_id
        JOIN services s ON s.id=NEW.service_id
        LEFT JOIN admins d ON d.id=NEW.doctor_id
        WHERE p.id=NEW.pet_id;
        IF ROW_COUNT() <> 1 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Missing service snapshot';
        END IF;
    END IF;
END$$

CREATE OR REPLACE TRIGGER appointment_history_after_update
AFTER UPDATE ON zapic_na_priem FOR EACH ROW
BEGIN
    IF NEW.status = 'done' AND OLD.status <> 'done' THEN
        INSERT INTO service_history
            (client_id, client_name, pet_id, pet_name, service_name, doctor_name,
             data_created, kol, total_price, address, pet_type, pol, pet_age,
             service_category, vid_priema)
        SELECT v.id, v.name, p.id, p.name, s.name, d.name, CURRENT_TIMESTAMP, 1,
               s.price, v.address, p.type, p.pol, p.age, s.category,
               CASE WHEN NEW.perv_priem_id IS NULL THEN 'Первичный' ELSE 'Повторный' END
        FROM pets p JOIN vladelci v ON v.id=p.client_id
        JOIN services s ON s.id=NEW.service_id
        LEFT JOIN admins d ON d.id=NEW.doctor_id
        WHERE p.id=NEW.pet_id;
        IF ROW_COUNT() <> 1 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Missing service snapshot';
        END IF;
    END IF;
END$$
DELIMITER ;
