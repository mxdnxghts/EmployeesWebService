CREATE TABLE departments(
	id INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
	name TEXT,
	phone VARCHAR(20)
);

CREATE TABLE passports(
	id INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
	type TEXT,
	number VARCHAR(20)
);

CREATE TABLE employees(
	id INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
	name TEXT,
	surname TEXT,
	phone VARCHAR(20),
	company_id INT,
	department_id INT,
	passport_id INT,
	CONSTRAINT fk_department FOREIGN KEY (department_id) REFERENCES departments(id),
	CONSTRAINT fk_passport FOREIGN KEY (passport_id) REFERENCES passports(id)
);