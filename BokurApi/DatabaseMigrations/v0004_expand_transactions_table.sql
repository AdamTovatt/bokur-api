ALTER TABLE bokur_transaction ADD COLUMN parent_transaction INT REFERENCES bokur_transaction(id) DEFAULT NULL;

ALTER TABLE bokur_transaction
ALTER COLUMN external_id DROP NOT NULL;