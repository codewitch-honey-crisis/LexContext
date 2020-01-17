using System;
using System.Collections.Generic;
using System.IO;
using LC;

namespace LexContextDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			// minifies JSON. Does so by parsing into an intermediary graph
			// this step wasn't required, but makes it easier to adapt
			// the code to a real world JSON parser

			// holds our json data
			IDictionary<string, object> json = null;
			
			// parse our file
			using (var pc = LexContext.CreateFrom(@"..\..\Burn Notice.2919.tv.json"))
				json = _ParseJsonObject(pc);
			
			// write our json data out
			_WriteJsonTo(json, Console.Out);
		}
		static object _ParseJson(LexContext pc)
		{
			// parses a JSON object, array, or value
			pc.TrySkipWhiteSpace();
			switch (pc.Current)
			{
				case '{':
					return _ParseJsonObject(pc);
				case '[':
					return _ParseJsonArray(pc);
				default:
					return _ParseJsonValue(pc);
			}

		}
		static IDictionary<string, object> _ParseJsonObject(LexContext pc)
		{
			// a JSON {} object - our objects are dictionaries
			var result = new Dictionary<string, object>();
			pc.TrySkipWhiteSpace();
			pc.Expecting('{');
			pc.Advance();
			pc.Expecting(); // expecting anything other than end of input
			while ('}' != pc.Current && -1 != pc.Current) // loop until } or end
			{
				pc.TrySkipWhiteSpace();
				// _ParseJsonValue parses any scalar value, but we only want 
				// a string so we check here that there's a quote mark to 
				// ensure the field will be a string.
				pc.Expecting('"');
				var fn = _ParseJsonValue(pc);
				pc.TrySkipWhiteSpace();
				pc.Expecting(':');
				pc.Advance();
				// add the next value to the dictionary
				result.Add(fn, _ParseJson(pc));
				pc.TrySkipWhiteSpace();
				pc.Expecting('}', ',');
				// skip commas
				if (',' == pc.Current) pc.Advance();
			}
			// make sure we're positioned on the end
			pc.Expecting('}');
			// ... and read past it
			pc.Advance();
			return result;
		}
		static IList<object> _ParseJsonArray(LexContext pc)
		{
			// a JSON [] array - our arrays are lists
			var result = new List<object>();
			pc.TrySkipWhiteSpace();
			pc.Expecting('[');
			pc.Advance();
			pc.Expecting(); // expect anything but end of input
							// loop until end of array or input
			while (-1 != pc.Current && ']' != pc.Current)
			{
				pc.TrySkipWhiteSpace();
				// add the next item
				result.Add(_ParseJson(pc));
				pc.TrySkipWhiteSpace();
				pc.Expecting(']', ',');
				// skip the comma
				if (',' == pc.Current) pc.Advance();
			}
			// ensure we're on the final position
			pc.Expecting(']');
			// .. and read past it
			pc.Advance();
			return result;
		}
		static string _ParseJsonValue(LexContext pc)
		{
			// parses a scalar JSON value, represented as a string
			// strings are returned quotes and all, with escapes 
			// embedded
			pc.TrySkipWhiteSpace();
			pc.Expecting(); // expect anything but end of input
			pc.ClearCapture();
			if ('\"' == pc.Current)
			{
				pc.Capture();
				pc.Advance();
				// reads until it finds a quote
				// using \ as an escape character
				// and consuming the final quote 
				// at the end
				pc.TryReadUntil('\"', '\\', true);
				// return what we read
				return pc.GetCapture();
			}
			pc.TryReadUntil(false, ',', '}', ']', ' ', '\t', '\r', '\n', '\v', '\f');
			return pc.GetCapture();
		}
		static void _WriteJsonTo(object json, TextWriter writer)
		{
			var d = json as IDictionary<string, object>;
			if (null != d)
				_WriteJsonObjectTo(d, writer);
			else
			{
				var l = json as IList<object>;
				if (null != l)
					_WriteJsonArrayTo(l, writer);
				else
					writer.Write(json);
			}
		}
		static void _WriteJsonObjectTo(IDictionary<string, object> json, TextWriter writer)
		{
			var delim = "{";
			foreach (var field in json)
			{
				writer.Write(delim);
				_WriteJsonTo(field.Key, writer);
				writer.Write(":");
				_WriteJsonTo(field.Value, writer);
				delim = ",";
			}
			if ("{" == delim)
				writer.Write(delim);
			writer.Write("}");
		}
		static void _WriteJsonArrayTo(IList<object> json, TextWriter writer)
		{
			var delim = "[";
			foreach (var item in json)
			{
				writer.Write(delim);
				_WriteJsonTo(item, writer);
				delim = ",";
			}
			if ("[" == delim)
				writer.Write(delim);
			
			writer.Write("]");
		}

	}
}
