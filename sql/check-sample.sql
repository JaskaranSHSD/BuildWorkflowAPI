SELECT c."Name" AS conn, a."Route", a."CommandText"
FROM "ApiDefinitions" a JOIN "Connections" c ON c."Id"=a."ConnectionId"
WHERE a."Id"=9001;