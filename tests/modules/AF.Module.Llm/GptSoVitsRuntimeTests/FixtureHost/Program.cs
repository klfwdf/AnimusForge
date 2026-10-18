using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
string token=Environment.GetEnvironmentVariable("AF_TTS_INSTANCE");
int index=Array.IndexOf(args,"--port");
var listener=new TcpListener(IPAddress.Loopback,int.Parse(args[index+1]));listener.Start();
while(true){using var client=await listener.AcceptTcpClientAsync();using var stream=client.GetStream();using var reader=new StreamReader(stream,Encoding.ASCII,false,1024,true);string line;bool authorized=false;while(!string.IsNullOrEmpty(line=await reader.ReadLineAsync())){if(line.Equals("X-AF-Instance: "+token,StringComparison.OrdinalIgnoreCase))authorized=true;}
byte[] data=Encoding.UTF8.GetBytes(authorized?JsonSerializer.Serialize(new {instance=token,af_runtime_api=1,package_id=new DirectoryInfo(AppContext.BaseDirectory).Name,engine="gpt-sovits-v2pro",state="ready",capabilities=new{audio="pcm16_mono_wav",batch_size=1,text_language_selection=File.Exists(Path.Combine(AppContext.BaseDirectory,"language-selection"))}}):"{}");
byte[] header=Encoding.ASCII.GetBytes("HTTP/1.1 "+(authorized?"200 OK":"403 Forbidden")+"\r\nContent-Type: application/json\r\nContent-Length: "+data.Length+"\r\nConnection: close\r\n\r\n");await stream.WriteAsync(header);await stream.WriteAsync(data);}
