ALTER TABLE bokur_account ADD COLUMN email VARCHAR;
UPDATE bokur_account SET email = 'adam@sakur.se' WHERE name = 'Adam';
UPDATE bokur_account SET email = 'oliver@sakur.se' WHERE name = 'Oliver';