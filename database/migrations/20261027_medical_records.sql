-- Run through scripts/Migrate-MedicalRecords.php with the API stopped.
CREATE TABLE medical_records (
    id INT NOT NULL PRIMARY KEY,
    jaloba TEXT NULL,
    diagnoz TEXT NULL,
    naz_lech TEXT NULL,
    procedure_done TEXT NULL,
    notes TEXT NULL,
    created_at DATETIME NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_medical_records_appointment FOREIGN KEY (id)
        REFERENCES zapic_na_priem(id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
