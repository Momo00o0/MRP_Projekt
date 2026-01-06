CREATE TABLE IF NOT EXISTS users (
  id            SERIAL PRIMARY KEY,
  guid          UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
  username      TEXT NOT NULL UNIQUE,
  password_hash TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS media_entries (
  id              SERIAL PRIMARY KEY,
  guid            UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
  title           TEXT NOT NULL,
  description     TEXT,
  release_year    INT,
  age_restriction INT,
  kind            TEXT NOT NULL,
  creator_id      INT NOT NULL REFERENCES users(id) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS ratings (
  id       SERIAL PRIMARY KEY,
  guid     UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
  user_id  INT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
  media_id INT NOT NULL REFERENCES media_entries(id) ON DELETE RESTRICT,
  stars    INT NOT NULL CHECK (stars BETWEEN 1 AND 5),
  comment  TEXT,
  timestamp TIMESTAMP NOT NULL,
  confirmed BOOLEAN not NULL
  
);
