-- ============================================================
-- Задание 2. База клиники на SQL (база clinic)
-- Выполнять в pgAdmin: Databases -> clinic -> Query Tool
-- Блоки выполнять по очереди: выделить блок и нажать F5
-- ============================================================


-- ------------------------------------------------------------
-- Шаг 2. Таблицы
-- Порядок важен: spravki ссылается на две другие таблицы, поэтому создаётся последней
-- ------------------------------------------------------------

-- Таблица пациентов. id выдаёт база сама (SERIAL), имя и дата рождения обязательны, телефон нет
CREATE TABLE patients (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    birth_date DATE NOT NULL,
    phone VARCHAR(20)
);

-- Каталог видов справок. CHECK: база не сохранит цену меньше или равную нулю
CREATE TABLE spravka_types (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    price NUMERIC(10, 2) NOT NULL CHECK (price > 0),
    valid_days INT NOT NULL
);

-- Выписанные справки. REFERENCES: внешние ключи, нельзя сослаться на несуществующего пациента или вид.
-- Цена хранится и здесь: это цена, за которую справку продали. Если каталог подорожает, старые справки не меняются
CREATE TABLE spravki (
    id SERIAL PRIMARY KEY,
    patient_id INT NOT NULL REFERENCES patients(id),
    spravka_type_id INT NOT NULL REFERENCES spravka_types(id),
    price NUMERIC(10, 2) NOT NULL,
    issued_at DATE NOT NULL DEFAULT CURRENT_DATE
);


-- ------------------------------------------------------------
-- Шаг 3. Данные
-- ------------------------------------------------------------

-- Пять пациентов из задания и один свой (Эрлан). У Бакыта нет телефона: NULL
INSERT INTO patients (name, birth_date, phone) VALUES
    ('Айбек Асанов', '1998-03-12', '0555123456'),
    ('Нургуль Токтосунова', '2003-07-25', '0700987654'),
    ('Бакыт Жумабаев', '1985-11-02', NULL),
    ('Айпери Садыкова', '2001-01-30', '0772345678'),
    ('Тимур Ким', '2006-09-14', '0509111222'),
    ('Эрлан Мамытов', '2010-04-18', '0701555777');

-- Каталог справок: четыре из задания
INSERT INTO spravka_types (name, price, valid_days) VALUES
    ('Справка 083', 100, 365),
    ('Санитарная книжка', 750, 365),
    ('Справка 086', 300, 180),
    ('Справка в бассейн', 200, 90);

-- Выписанные справки: (id пациента, id вида, цена, дата). Последняя строка своя
INSERT INTO spravki (patient_id, spravka_type_id, price, issued_at) VALUES
    (1, 1, 100, '2026-10-01'),
    (1, 4, 200, '2026-10-15'),
    (2, 2, 750, '2026-10-03'),
    (2, 3, 300, '2026-11-02'),
    (4, 3, 300, '2026-11-05'),
    (5, 4, 200, '2026-11-06'),
    (5, 1, 100, '2026-11-10'),
    (6, 4, 200, '2026-11-11');


-- ------------------------------------------------------------
-- Шаг 4. JOIN трёх таблиц и статистика
-- ------------------------------------------------------------

-- Справки с именем пациента и названием справки вместо номеров, по дате выписки
SELECT spravki.id, patients.name AS patient, spravka_types.name AS spravka, spravki.price, spravki.issued_at
FROM spravki
JOIN patients ON spravki.patient_id = patients.id
JOIN spravka_types ON spravki.spravka_type_id = spravka_types.id
ORDER BY spravki.issued_at;

-- Статистика для главврача: сколько справок и на какую сумму
SELECT COUNT(*) AS spravki_count, SUM(price) AS total FROM spravki;


-- ------------------------------------------------------------
-- Шаг 5. Запросы для клиники
-- ------------------------------------------------------------

-- 5.1 Регистратура: все пациенты по алфавиту
SELECT * FROM patients ORDER BY name;

-- 5.2 Пациенты, родившиеся в 2000 году и позже
SELECT * FROM patients WHERE birth_date >= '2000-01-01';

-- 5.3 Пациенты без телефона.
-- Вариант "= NULL" ничего не находит: NULL значит "неизвестно", а сравнение
-- с неизвестным даёт не "да", а "неизвестно", и строка не попадает в результат.
-- Для NULL нужен особый оператор IS NULL.
SELECT * FROM patients WHERE phone = NULL;     -- 0 строк (неправильно)
SELECT * FROM patients WHERE phone IS NULL;    -- Бакыт Жумабаев (правильно)

-- 5.4 Поиск пациента по части имени. % в шаблоне значит "любые символы".
-- LIKE различает регистр: 'ай' с маленькой буквы в именах нет, поэтому ничего не найдёт.
-- ILIKE регистр не различает: найдёт Айбек и Айпери.
SELECT * FROM patients WHERE name LIKE '%ай%';
SELECT * FROM patients WHERE name ILIKE '%ай%';

-- 5.5 Карточка пациента: все справки пациента с id 1, с названием справки, ценой и датой
SELECT spravka_types.name AS spravka, spravki.price, spravki.issued_at
FROM spravki
JOIN spravka_types ON spravki.spravka_type_id = spravka_types.id
WHERE spravki.patient_id = 1
ORDER BY spravki.issued_at;

-- 5.6 Справки, выписанные в ноябре 2026
SELECT * FROM spravki
WHERE issued_at >= '2026-11-01' AND issued_at < '2026-12-01';

-- 5.7 Самая дорогая справка в каталоге: сортировка по убыванию цены и только одна строка
SELECT * FROM spravka_types ORDER BY price DESC LIMIT 1;

-- 5.8 Главврач: сколько справок выписано в ноябре и на какую сумму
SELECT COUNT(*) AS spravki_count, SUM(price) AS total
FROM spravki
WHERE issued_at >= '2026-11-01' AND issued_at < '2026-12-01';

-- 5.9 Каталог подорожал: поднимаем цену справки 083 со 100 до 120
UPDATE spravka_types SET price = 120 WHERE id = 1;
-- Проверка: цена в каталоге новая, а в уже выданных справках осталась 100
SELECT * FROM spravka_types WHERE id = 1;
SELECT * FROM spravki WHERE spravka_type_id = 1;

-- 5.10 Удаляем одну справку (последнюю, id 8)
DELETE FROM spravki WHERE id = 8;

-- 5.11 Бонус: сколько справок у каждого пациента.
-- LEFT JOIN оставляет и тех, у кого справок нет (у них будет 0)
SELECT patients.name, COUNT(spravki.id) AS spravki_count
FROM patients
LEFT JOIN spravki ON spravki.patient_id = patients.id
GROUP BY patients.id, patients.name
ORDER BY spravki_count DESC;


-- ------------------------------------------------------------
-- Шаг 6. База защищает данные
-- Следующие запросы специально вызывают ошибки. Они закомментированы, чтобы
-- не останавливать запуск всего файла. Выполняйте по одному:
-- уберите "-- " перед запросом, выделите его и нажмите F5.
-- ------------------------------------------------------------

-- 6.1 Справка несуществующему пациенту.
-- Ошибка: violates foreign key constraint "spravki_patient_id_fkey", Key (patient_id)=(999) is not present in table "patients"
-- INSERT INTO spravki (patient_id, spravka_type_id, price) VALUES (999, 1, 100);

-- 6.2 Отрицательная цена.
-- Ошибка: violates check constraint "spravka_types_price_check"
-- INSERT INTO spravka_types (name, price, valid_days) VALUES ('Справка в лагерь', -500, 30);

-- 6.3 Пациент без имени.
-- Ошибка: null value in column "name" of relation "patients" violates not-null constraint
-- INSERT INTO patients (birth_date) VALUES ('2000-01-01');

-- 6.4 Удаление пациента, на которого есть справки.
-- Ошибка: update or delete on table "patients" violates foreign key constraint "spravki_patient_id_fkey" on table "spravki"
-- DELETE FROM patients WHERE id = 1;

-- 6.5 Номера с пропусками. Неудачные вставки выше тоже забирают номер из счётчика,
-- поэтому следующая справка получает id с пропуском, но номера никогда не повторяются.
-- Баг "новая справка открывается как чужая" здесь невозможен.
INSERT INTO spravki (patient_id, spravka_type_id, price) VALUES (3, 2, 750);
SELECT * FROM spravki WHERE patient_id = 3;
