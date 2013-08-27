using System.Collections;
using SimpleJson;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace Pomelo.DotNetClient
{
	public class PomeloClient : IDisposable
	{
		public const string EVENT_DISCONNECT = "disconnect";
		private string _host = "";
		private int _port = 0;
		private EventManager eventManager;
		private Socket socket;
		private Protocol protocol;
		private bool disposed = false;
		private uint reqId = 1;
		public PomeloClient(string host, int port) {
			this.eventManager = new EventManager();
			this.socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
			this.protocol = new Protocol(this, socket);
			_host = host;
			_port = port;
		}

		private bool tryConnect(string host, int port) {
	        IPEndPoint ie = new IPEndPoint(IPAddress.Parse(host), port);
	        try {
	            this.socket.Connect(ie);
				return true;
	        }
	        catch (SocketException e) {
				Debug.Log(e.ToString());
	            return false;
	        }
		}
		
		private bool tryHandshake(JsonObject user, Action<JsonObject> handshakeCallback)
		{
			try{
				protocol.start(user, handshakeCallback);
				return true;
			}
			catch(Exception e){
				Debug.Log(e.ToString());
				return false;
			}
		}
			
		public void connect(){
			protocol.start(null, null);
		}
		
		public void connect(JsonObject user){
			protocol.start(user, null);
		}
		
		public void connect(Action<JsonObject> handshakeCallback){
			protocol.start(null, handshakeCallback);
		}
		
		public bool connect(JsonObject user, Action<JsonObject> handshakeCallback){
			bool c = tryConnect(_host, _port);
			bool h = false;
			if (c == true)
			{
				h = tryHandshake(user,handshakeCallback);
			}
			if (!c || !h)
			{
				handshakeCallback.Invoke(null);
			}
			return h;
		}

		public void request(string route, Action<JsonObject> action){
			this.request(route, new JsonObject(), action);
		}
		
		public void request(string route, JsonObject msg, Action<JsonObject> action){
			this.eventManager.AddCallBack(reqId, action);
			protocol.send (route, reqId, msg);

			reqId++;
		}
		
		public void notify(string route, JsonObject msg){
			protocol.send (route, msg);
		}
		
		public void on(string eventName, Action<JsonObject> action){
			eventManager.AddOnEvent(eventName, action);
		}

		internal void processMessage(Message msg){
			Debug.Log("processMessage");
			if(msg.type == MessageType.MSG_RESPONSE){
				eventManager.InvokeCallBack(msg.id, msg.data);
			}else if(msg.type == MessageType.MSG_PUSH){
				eventManager.InvokeOnEvent(msg.route, msg.data);
			}
		}

		public void disconnect(){
			Dispose ();
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// The bulk of the clean-up code 
		protected virtual void Dispose(bool disposing) {
			if(this.disposed) return;
			if (disposing) {
				// free managed resources
				this.protocol.close();
				try
				{
					this.socket.Shutdown(SocketShutdown.Both);
				}
				catch (Exception e)
				{
					Debug.Log("???"+e.ToString());
				}
				this.socket.Close();
				this.disposed = true;
				//Call disconnect callback
				eventManager.InvokeOnEvent(EVENT_DISCONNECT, null);
			}
		}
	}
}

