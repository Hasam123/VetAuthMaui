-- Remove source identifiers from snapshots; completed appointments cannot reopen.
DELIMITER $$
CREATE OR REPLACE TRIGGER appointment_history_no_reopen
BEFORE UPDATE ON zapic_na_priem FOR EACH ROW
BEGIN
    IF OLD.status = 'done' AND NEW.status <> 'done' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Completed appointments cannot be reopened';
    END IF;
END$$

CREATE OR REPLACE TRIGGER appointment_history_after_insert
AFTER INSERT ON zapic_na_priem FOR EACH ROW
BEGIN
    IF NEW.status = 'done' THEN
        INSERT INTO service_history
            (client_id, client_name, pet_id, pet_name, service_name,
             doctor_name, performed_at, quantity, total_price)
        SELECT v.id, v.name, p.id, p.name, s.name, doctor.name, CURRENT_TIMESTAMP, 1, s.price
        FROM pets p JOIN vladelci v ON v.id = p.client_id
        JOIN services s ON s.id = NEW.service_id
        LEFT JOIN admins doctor ON doctor.id = NEW.doctor_id
        WHERE p.id = NEW.pet_id;
        IF ROW_COUNT() <> 1 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Missing service snapshot';
        END IF;
    END IF;
END$$

CREATE OR REPLACE TRIGGER appointment_history_after_update
AFTER UPDATE ON zapic_na_priem FOR EACH ROW
BEGIN
    IF NEW.status = 'done' AND OLD.status <> 'done' THEN
        INSERT INTO service_history
            (client_id, client_name, pet_id, pet_name, service_name,
             doctor_name, performed_at, quantity, total_price)
        SELECT v.id, v.name, p.id, p.name, s.name, doctor.name, CURRENT_TIMESTAMP, 1, s.price
        FROM pets p JOIN vladelci v ON v.id = p.client_id
        JOIN services s ON s.id = NEW.service_id
        LEFT JOIN admins doctor ON doctor.id = NEW.doctor_id
        WHERE p.id = NEW.pet_id;
        IF ROW_COUNT() <> 1 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Missing service snapshot';
        END IF;
    END IF;
END$$
DELIMITER ;

ALTER TABLE service_history DROP INDEX ix_service_history_service_date,
    DROP COLUMN appointment_id, DROP COLUMN service_id;
