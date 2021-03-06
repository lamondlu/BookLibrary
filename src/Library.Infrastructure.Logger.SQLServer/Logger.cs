﻿using Library.Domain.Core;
using Library.Domain.Core.Commands;
using Library.Domain.Core.Models;
using Library.Infrastructure.DataPersistence.Core.SQLServer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Library.Infrastructure.Logger.SQLServer
{
	public class Logger : ILogger
	{
		private ILogDBConnectionStringProvider _logDBConnectionStringProvider = null;

		public Logger(ILogDBConnectionStringProvider logDBConnectionStringProvider)
		{
			_logDBConnectionStringProvider = logDBConnectionStringProvider;
		}

		public void Command(Guid commandUniqueId, string commandName, string eventName, string message, bool isSuccess, LogType logType, object data)
		{
			var dbHelper = new DbHelper(_logDBConnectionStringProvider.ConnectionString);
			var sql = "INSERT INTO CommandLogs(Id, LogType, CommandName, CommandUniqueId, EventName, IsSuccess, Message, CreatedOn, Data) VALUES(@id, @logType, @commandName, @commandUniqueId, @eventName, @isSuccess, @message, @createdOn, @data)";

			dbHelper.ExecuteNonQuery(sql, new List<SqlParameter>{
				new SqlParameter{ ParameterName = "@id", SqlDbType = SqlDbType.UniqueIdentifier, Value= Guid.NewGuid()},
				new SqlParameter{ ParameterName = "@logType", SqlDbType = SqlDbType.Int, Value= Convert.ToInt32(logType)},
				new SqlParameter{ ParameterName = "@commandName", SqlDbType = SqlDbType.NVarChar, Value= commandName},
				new SqlParameter{ ParameterName = "@commandUniqueId", SqlDbType = SqlDbType.UniqueIdentifier, Value= commandUniqueId},
				new SqlParameter{ ParameterName = "@eventName", SqlDbType = SqlDbType.NVarChar, Value= eventName},
				new SqlParameter{ ParameterName = "@isSuccess", SqlDbType = SqlDbType.Bit, Value= isSuccess},
				new SqlParameter{ ParameterName = "@message", SqlDbType = SqlDbType.NVarChar, Value= message},
				new SqlParameter{ ParameterName = "@createdOn", SqlDbType = SqlDbType.DateTime2, Value= DateTime.Now},
				new SqlParameter{ ParameterName = "@data", SqlDbType = SqlDbType.NVarChar, Value= JsonConvert.SerializeObject(data)}
			}.ToArray());
		}

		public void EventError<T>(T eventObject, string message) where T : DomainEvent
		{
			Command(eventObject.CommandUniqueId, string.Empty, eventObject.EventKey, message, false, LogType.Error, eventObject);
		}

		public void EventInfo<T>(T eventObject, string message) where T : DomainEvent
		{
			Command(eventObject.CommandUniqueId, string.Empty, eventObject.EventKey, message, true, LogType.Info, eventObject);
		}

		public void EventWarning<T>(T eventObject, string message) where T : DomainEvent
		{
			Command(eventObject.CommandUniqueId, string.Empty, eventObject.EventKey, message, false, LogType.Warning, eventObject);
		}

		public void CommandError<T>(T command, string message) where T : CommonCommand
		{
			Command(command.CommandUniqueId, command.CommandKey, string.Empty, message, false, LogType.Error, command);
		}

		public void CommandInfo<T>(T command, string message) where T : CommonCommand
		{
			Command(command.CommandUniqueId, command.CommandKey, string.Empty, message, true, LogType.Info, command);
		}

		public void CommandWarning<T>(T command, string message) where T : CommonCommand
		{
			Command(command.CommandUniqueId, command.CommandKey, string.Empty, message, false, LogType.Warning, command);
		}

		public List<CommandLogModel> GetCommandLogs()
		{
			var sql = "SELECT * FROM CommandLogs WHERE CommandName <> ''";

			var dbHelper = new DbHelper(_logDBConnectionStringProvider.ConnectionString);
			var dataTable = dbHelper.ExecuteDataTable(sql);

			return dataTable.ConvertTo();
		}

		public List<CommandLogModel> GetEventLogs(Guid commandUniqueId)
		{
			var sql = "SELECT * FROM CommandLogs WHERE CommandUniqueId = @commandUniqueId AND CommandName = '' ORDER BY CreatedOn DESC";

			var dbHelper = new DbHelper(_logDBConnectionStringProvider.ConnectionString);
			var dataTable = dbHelper.ExecuteDataTable(sql, new SqlParameter
			{
				ParameterName = "@commandUniqueId",
				SqlDbType = SqlDbType.UniqueIdentifier,
				Value = commandUniqueId
			});

			return dataTable.ConvertTo();
		}
	}
}