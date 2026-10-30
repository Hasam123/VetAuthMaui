-- Additional immutable source copies. Do not backfill existing history.
ALTER TABLE service_history
    ADD COLUMN IF NOT EXISTS phone VARCHAR(15) NULL,
    ADD COLUMN IF NOT EXISTS weight DECIMAL(5,2) NULL,
    ADD COLUMN IF NOT EXISTS data_zabol DATE NULL,
    ADD COLUMN IF NOT EXISTS jaloba TEXT NULL,
    ADD COLUMN IF NOT EXISTS pred_diag TEXT NULL,
    ADD COLUMN IF NOT EXISTS diagnoz TEXT NULL,
    ADD COLUMN IF NOT EXISTS klin_prizn TEXT NULL,
    ADD COLUMN IF NOT EXISTS naz_lech TEXT NULL,
    ADD COLUMN IF NOT EXISTS procedure_done TEXT NULL,
    ADD COLUMN IF NOT EXISTS ishod VARCHAR(100) NULL,
    ADD COLUMN IF NOT EXISTS notes TEXT NULL;

DELIMITER $$
CREATE OR REPLACE TRIGGER appointment_history_after_insert
AFTER INSERT ON zapic_na_priem FOR EACH ROW
BEGIN
    IF NEW.status = 'done' THEN
        INSERT INTO service_history
            (client_id, client_name, pet_id, pet_name, service_name, doctor_name,
             data_created, kol, total_price, address, pet_type, pol, pet_age,
             service_category, vid_priema, phone, weight, data_zabol, jaloba, pred_diag,
             diagnoz, klin_prizn, naz_lech, procedure_done, ishod, notes)
        SELECT v.id, v.name, p.id, p.name, s.name, d.name, CURRENT_TIMESTAMP, 1,
               s.price, v.address, p.type, p.pol, p.age, s.category,
               CASE WHEN NEW.perv_priem_id IS NULL THEN 'Первичный' ELSE 'Повторный' END,
               v.phone, p.weight, m.data_zabol, m.jaloba, m.pred_diag, m.diagnoz,
               m.klin_prizn, m.naz_lech, m.procedure_done, m.ishod, m.notes
        FROM pets p JOIN vladelci v ON v.id=p.client_id
        JOIN services s ON s.id=NEW.service_id
        LEFT JOIN admins d ON d.id=NEW.doctor_id
        LEFT JOIN medical_records m ON m.id=NEW.id
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
             service_category, vid_priema, phone, weight, data_zabol, jaloba, pred_diag,
             diagnoz, klin_prizn, naz_lech, procedure_done, ishod, notes)
        SELECT v.id, v.name, p.id, p.name, s.name, d.name, CURRENT_TIMESTAMP, 1,
               s.price, v.address, p.type, p.pol, p.age, s.category,
               CASE WHEN NEW.perv_priem_id IS NULL THEN 'Первичный' ELSE 'Повторный' END,
               v.phone, p.weight, m.data_zabol, m.jaloba, m.pred_diag, m.diagnoz,
               m.klin_prizn, m.naz_lech, m.procedure_done, m.ishod, m.notes
        FROM pets p JOIN vladelci v ON v.id=p.client_id
        JOIN services s ON s.id=NEW.service_id
        LEFT JOIN admins d ON d.id=NEW.doctor_id
        LEFT JOIN medical_records m ON m.id=NEW.id
        WHERE p.id=NEW.pet_id;
        IF ROW_COUNT() <> 1 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Missing service snapshot';
        END IF;
    END IF;
END$$
DELIMITER ;
