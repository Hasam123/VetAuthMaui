-- Existing and new doctors are active by default.
ALTER TABLE admins
    ADD COLUMN IF NOT EXISTS is_active TINYINT(1) NOT NULL DEFAULT 1 AFTER name;
