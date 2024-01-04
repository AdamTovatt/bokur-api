CREATE TABLE bokur_account (
	id SERIAL PRIMARY KEY,
	name VARCHAR NOT NULL
);

CREATE TABLE bokur_transaction (
	id SERIAL PRIMARY KEY,
	external_id VARCHAR NOT NULL UNIQUE,
	name VARCHAR NOT NULL,
	value NUMERIC NOT NULL,
	date TIMESTAMP NOT NULL,
	associated_file_name VARCHAR DEFAULT NULL,
	affected_account INT REFERENCES bokur_account(id),
	ignored BOOLEAN NOT NULL DEFAULT FALSE
);