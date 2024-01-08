ALTER TABLE bokur_transaction ADD COLUMN sibling INT REFERENCES bokur_transaction(id) DEFAULT NULL;
ALTER TABLE bokur_transaction RENAME COLUMN parent_transaction TO parent;