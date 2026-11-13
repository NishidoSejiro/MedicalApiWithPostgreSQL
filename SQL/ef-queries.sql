-- ============================================================
-- Задание 5. SQL, который EF Core отправляет в базу для PatientsController
--
-- ВАЖНО: запросы ниже написаны в том виде, в каком EF Core обычно их генерирует.
-- Перед сдачей замените каждый запрос текстом из СВОЕЙ консоли (там, где запущен dotnet run):
-- найдите строку "Executed DbCommand" и скопируйте SQL под ней. Имена параметров
-- (@id, @p0) и порядок столбцов в вашей версии могут немного отличаться.
-- ============================================================


-- GET /api/patients
-- Нет WHERE: возвращаются все пациенты, отсортированные по имени
SELECT p."Id", p."Address", p."BirthDate", p."DistrictNumber", p."HasInsurance", p."Name", p."Phone"
FROM "Patients" AS p
ORDER BY p."Name";


-- GET /api/patients?name=ай
-- Появился WHERE с ILIKE: поиск без учёта регистра выполняет сама база
SELECT p."Id", p."Address", p."BirthDate", p."DistrictNumber", p."HasInsurance", p."Name", p."Phone"
FROM "Patients" AS p
WHERE p."Name" ILIKE @name
ORDER BY p."Name";


-- GET /api/patients/1
-- Есть WHERE по Id и LIMIT 1: нужна одна строка, а не вся таблица
SELECT p."Id", p."Address", p."BirthDate", p."DistrictNumber", p."HasInsurance", p."Name", p."Phone"
FROM "Patients" AS p
WHERE p."Id" = @id
LIMIT 1;


-- POST /api/patients
-- Столбца Id в INSERT нет: номер выдаёт база. RETURNING "Id" возвращает выданный номер,
-- EF Core записывает его в patient.Id, поэтому после SaveChangesAsync id уже заполнен
INSERT INTO "Patients" ("Address", "BirthDate", "DistrictNumber", "HasInsurance", "Name", "Phone")
VALUES (@p0, @p1, @p2, @p3, @p4, @p5)
RETURNING "Id";


-- PUT /api/patients/1
-- Сначала SELECT (контроллер ищет пациента), потом UPDATE.
-- В UPDATE только изменённые столбцы: если поменяли одно поле, то в SET будет один столбец.
-- Если отправить те же данные, что уже лежат в базе, UPDATE не отправляется вообще:
-- EF Core сравнил значения и увидел, что менять нечего
SELECT p."Id", p."Address", p."BirthDate", p."DistrictNumber", p."HasInsurance", p."Name", p."Phone"
FROM "Patients" AS p
WHERE p."Id" = @id
LIMIT 1;

UPDATE "Patients" SET "Name" = @p0
WHERE "Id" = @p1;


-- DELETE /api/patients/1
-- Сначала SELECT пациента, потом две проверки AnyAsync (есть ли справки и приёмы),
-- и только если их нет, DELETE
SELECT p."Id", p."Address", p."BirthDate", p."DistrictNumber", p."HasInsurance", p."Name", p."Phone"
FROM "Patients" AS p
WHERE p."Id" = @id
LIMIT 1;

SELECT EXISTS (
    SELECT 1 FROM "Spravki" AS s WHERE s."PatientId" = @id);

SELECT EXISTS (
    SELECT 1 FROM "Appointments" AS a WHERE a."PatientId" = @id);

DELETE FROM "Patients"
WHERE "Id" = @p0;


-- ============================================================
-- Ответы на вопросы из задания
-- ============================================================

-- Чем SQL от GET /api/patients отличается от GET /api/patients/1?
-- У первого нет WHERE, он возвращает все строки таблицы по порядку имён.
-- У второго есть WHERE "Id" = @id и LIMIT 1: база ищет одну строку по номеру.

-- Как приложение узнаёт id, который выдала база при POST?
-- В INSERT нет столбца Id. База сама берёт следующий номер, а RETURNING "Id" отправляет его
-- обратно в приложение. EF Core записывает этот номер в объект patient.Id.

-- Почему в логе стоит '?' вместо значений?
-- EF Core по умолчанию не пишет в логи значения параметров: там могут быть
-- персональные данные пациентов (имя, телефон, дата рождения). Чтобы на своём
-- компьютере видеть значения, можно временно дописать .EnableSensitiveDataLogging()
-- после UseNpgsql(...). В рабочем проекте это не включают.
