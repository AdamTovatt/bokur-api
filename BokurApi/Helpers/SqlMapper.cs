﻿using NpgsqlTypes;

namespace BokurApi.Helpers
{
    public static class SqlMapper
    {
        private static Dictionary<Type, NpgsqlDbType> typeMap = new Dictionary<Type, NpgsqlDbType>()
        {
            [typeof(byte)] = NpgsqlDbType.Smallint,
            [typeof(sbyte)] = NpgsqlDbType.Smallint,
            [typeof(short)] = NpgsqlDbType.Smallint,
            [typeof(ushort)] = NpgsqlDbType.Smallint,
            [typeof(int)] = NpgsqlDbType.Integer,
            [typeof(uint)] = NpgsqlDbType.Integer,
            [typeof(long)] = NpgsqlDbType.Bigint,
            [typeof(ulong)] = NpgsqlDbType.Bigint,
            [typeof(float)] = NpgsqlDbType.Real,
            [typeof(double)] = NpgsqlDbType.Double,
            [typeof(decimal)] = NpgsqlDbType.Numeric,
            [typeof(bool)] = NpgsqlDbType.Boolean,
            [typeof(string)] = NpgsqlDbType.Varchar,
            [typeof(System.String)] = NpgsqlDbType.Varchar,
            [typeof(char)] = NpgsqlDbType.Char,
            [typeof(Guid)] = NpgsqlDbType.Uuid,
            [typeof(DateTime)] = NpgsqlDbType.Timestamp,
            [typeof(DateTimeOffset)] = NpgsqlDbType.TimestampTz,
            [typeof(byte[])] = NpgsqlDbType.Bytea,
            [typeof(byte?)] = NpgsqlDbType.Smallint,
            [typeof(sbyte?)] = NpgsqlDbType.Smallint,
            [typeof(short?)] = NpgsqlDbType.Smallint,
            [typeof(ushort?)] = NpgsqlDbType.Smallint,
            [typeof(int?)] = NpgsqlDbType.Integer,
            [typeof(uint?)] = NpgsqlDbType.Integer,
            [typeof(long?)] = NpgsqlDbType.Bigint,
            [typeof(ulong?)] = NpgsqlDbType.Bigint,
            [typeof(float?)] = NpgsqlDbType.Real,
            [typeof(double?)] = NpgsqlDbType.Double,
            [typeof(decimal?)] = NpgsqlDbType.Numeric,
            [typeof(bool?)] = NpgsqlDbType.Boolean,
            [typeof(char?)] = NpgsqlDbType.Char,
            [typeof(Guid?)] = NpgsqlDbType.Uuid,
            [typeof(DateTime?)] = NpgsqlDbType.Timestamp,
            [typeof(DateTimeOffset?)] = NpgsqlDbType.TimestampTz,
        };

        public static NpgsqlDbType GetNpgsqlDbType(Type type)
        {
            if (typeMap.ContainsKey(type))
                return typeMap[type];
            else
                throw new Exception($"Type {type} is not supported by Npgsql.");
        }
    }
}
