// **********************************************************************
//
// Copyright (c) 2003
// ZeroC, Inc.
// Billerica, MA, USA
//
// All Rights Reserved.
//
// Ice is free software; you can redistribute it and/or modify it under
// the terms of the GNU General Public License version 2 as published by
// the Free Software Foundation.
//
// **********************************************************************

//
// Work-around for bug in .NET Socket class implementation.
//
#define ENDPOINTBUG

namespace IceInternal
{

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

public sealed class Network
{

    const int WSAEINTR = 10004;
    const int WSAEFAULT = 10014;
    const int WSAEWOULDBLOCK = 10035;
    const int WSAEMSGSIZE = 10040;
    const int WSAENETUNREACH = 10051;
    const int WSAECONNABORTED = 10053;
    const int WSAECONNRESET = 10054;
    const int WSAENOBUFS = 10055;
    const int WSAENOTCONN = 10057;
    const int WSAESHUTDOWN = 10058;
    const int WSAETIMEDOUT = 10060;
    const int WSAECONNREFUSED = 100061;
    const int WSATRY_AGAIN = 11002;

    public static bool
    interrupted(Win32Exception ex)
    {
        return ex.NativeErrorCode == WSAEINTR;
    }

    public static bool
    acceptInterrupted(Win32Exception ex)
    {
        if(interrupted(ex))
	{
	    return true;
	}
	int error = ex.NativeErrorCode;
	return error == WSAECONNABORTED ||
	       error == WSAECONNRESET ||
	       error == WSAETIMEDOUT;
    }

    public static bool
    noBuffers(Win32Exception ex)
    {
        int error = ex.NativeErrorCode;
	return error == WSAENOBUFS ||
	       error == WSAEFAULT;
    }

    public static bool
    wouldBlock(Win32Exception ex)
    {
        return ex.NativeErrorCode == WSAEWOULDBLOCK;
    }

    public static bool
    connectFailed(Win32Exception ex)
    {
        int error = ex.NativeErrorCode;
	return error == WSAECONNREFUSED ||
	       error == WSAETIMEDOUT ||
	       error == WSAENETUNREACH ||
	       error == WSAECONNRESET ||
	       error == WSAESHUTDOWN ||
	       error == WSAECONNABORTED;
    }

    public static bool
    connectInProgress(Win32Exception ex)
    {
        return ex.NativeErrorCode == WSAEWOULDBLOCK;
    }

    public static bool
    connectionLost(Win32Exception ex)
    {
        int error = ex.NativeErrorCode;
	return error == WSAECONNRESET ||
	       error == WSAESHUTDOWN ||
	       error == WSAECONNABORTED;
    }
    
    public static bool
    notConnected(Win32Exception ex)
    {
        return ex.NativeErrorCode == WSAENOTCONN;
    }

    public static bool
    recvTruncated(Win32Exception ex)
    {
        return ex.NativeErrorCode == WSAEMSGSIZE;
    }

    public static Socket
    createSocket(bool udp)
    {
	Socket socket;

	try
	{
	    if(udp)
	    {
		socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
	    }
	    else
	    {
		socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	    }
	}
	catch(SocketException ex)
	{
	    throw new Ice.SocketException("Cannot create socket", ex);
	}

	if(!udp)
	{
	    try
	    {
		setTcpNoDelay(socket);
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
	    }
	    catch(SystemException ex)
	    {
		throw new Ice.SocketException("Cannot set socket options", ex);
	    }
	}
	return socket;
    }

    public static void
    closeSocket(Socket socket)
    {
        if(socket == null)
	{
	    return;
	}
	try
	{
	    socket.Close();
	}
	catch(SocketException ex)
	{
	    throw new Ice.SocketException("Cannot close socket", ex);
	}
    }

    public static void
    setBlock(Socket socket, bool block)
    {
        socket.Blocking = block;
    }

    public static void
    setTcpNoDelay(Socket socket)
    {
	try
	{
	    socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
	}
	catch(SystemException ex)
	{
	    throw new Ice.SocketException("Cannot set NoDelay option", ex);
	}
    }

    public static void
    setKeepAlive(Socket socket)
    {
	try
	{
	    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
	}
	catch(SystemException ex)
	{
	    throw new Ice.SocketException("Cannot set KeepAlive option", ex);
	}
    }

    public static void
    setSendBufferSize(Socket socket, int sz)
    {
	try
	{
	    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, sz);
	}
	catch(SystemException ex)
	{
	    throw new Ice.SocketException("Cannot set send buffer size", ex);
	}
    }

    public static int
    getSendBufferSize(Socket socket)
    {
	int sz;
        try
	{
	    sz = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
	}
	catch(SystemException ex)
	{
	    throw new Ice.SocketException("Cannot read send buffer size", ex);
	}
	return sz;
    }
    
    public static void
    setRecvBufferSize(Socket socket, int sz)
    {
	try
	{
	    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, sz);
	}
	catch(SystemException ex)
	{
	    throw new Ice.SocketException("Cannot set receive buffer size", ex);
	}
    }

    public static int
    getRecvBufferSize(Socket socket)
    {
	int sz = 0;
        try
	{
	    sz = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
	}
	catch(System.Exception ex)
	{
	    Ice.SocketException se = new Ice.SocketException("Cannot read receive buffer size", ex);
	}
	return sz;
    }

/*
    public static void
    setSendTimeout(Socket socket, int timeout)
    {
	try
	{
	    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, timeout);
	}
	catch(SocketException ex)
	{
	    throw new Ice.SocketException(ex);
	}
    }

    public static void
    setRecvTimeout(Socket socket, int timeout)
    {
	try
	{
	    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeout);
	}
	catch(SocketException ex)
	{
	    throw new Ice.SocketException(ex);
	}
    }
*/
    
    public static IPEndPoint
    doBind(Socket socket, EndPoint addr)
    {
	try
	{
	    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
	    socket.Bind(addr);
	    return (IPEndPoint)socket.LocalEndPoint;
	}
	catch(SystemException ex)
	{
	    throw new Ice.SocketException("Cannot bind", ex);
	}
    }
    
    public static void
    doListen(Socket socket, int backlog)
    {
    repeatListen:

        try
	{
	    socket.Listen(backlog);
	}
	catch(SocketException ex)
	{
	    if(interrupted(ex))
	    {
	        goto repeatListen;
	    }
	    try
	    {
	        socket.Close();
	    }
	    catch(SystemException)
	    {
	        // ignore
	    }
	    throw new Ice.SocketException("Cannot listen", ex);
	}
    }

    public static void
    doConnect(Socket socket, EndPoint addr, int timeout)
    {
	setSendBufferSize(socket, 64 * 1024);

    repeatConnect:
	try
	{
	    socket.Connect(addr);
	}
	catch(SocketException ex)
	{
	    if(interrupted(ex))
	    {
	        goto repeatConnect;
	    }
	    if(!connectInProgress(ex))
	    {
		throw new Ice.ConnectFailedException("Connect failed", ex);
	    }

	repeatSelect:
	    bool ready;
	    bool error;
	    try
	    {
	        ArrayList writeList = new ArrayList();
		writeList.Add(socket);
		ArrayList errorList = new ArrayList();
		errorList.Add(socket);
		doSelect(null, writeList, errorList, timeout);
		ready = writeList.Count != 0;
		error = errorList.Count != 0;
		Debug.Assert(!(ready && error));
	    }
	    catch(SocketException e)
	    {
		if(interrupted(e))
		{
		    goto repeatSelect;
		}
	        throw new Ice.SocketException(e);
	    }
	    if(error)
	    {
		throw new Ice.ConnectFailedException("Connect failed: connection refused");
	    }
	    if(!ready)
	    {
		try
		{
		    socket.Close();
		}
		catch(SocketException)
		{
		    // ignore
		}
		throw new Ice.ConnectFailedException("Connect timed out after " + timeout + "msec");
	    }
	}
    }
    
    public static Socket
    doAccept(Socket socket, int timeout)
    {
        Socket ret = null;

    repeatAccept:
	try
	{
	    ret = socket.Accept();
	}
	catch(SocketException ex)
	{
	    if(acceptInterrupted(ex))
	    {
		goto repeatAccept;
	    }
	    if(wouldBlock(ex))
	    {
	    repeatSelect:
	        ArrayList readList = new ArrayList();
		readList.Add(socket);
		try
		{
		    doSelect(readList, null, null, timeout);
		}
		catch(SystemException se)
		{
		    if(interrupted(ex))
		    {
		        goto repeatSelect;
		    }
		    throw new Ice.SocketException("select failed", se);
		}

		if(readList.Count == 0)
		{
		    throw new Ice.TimeoutException();
		}

		goto repeatAccept;
	    }
	}

	setTcpNoDelay(ret);
	setKeepAlive(ret);
	setSendBufferSize(ret, 64 * 1024);
	
	return ret;
    }
    
    public static void
    doSelect(IList checkRead, IList checkWrite, IList checkError, int milliSeconds)
    {
	ArrayList cr;
	ArrayList cw;
	ArrayList ce;

        if(milliSeconds < 0)
	{
	    //
	    // Socket.Select() returns immediately if the timeout is < 0 (instead
	    // of blocking indefinitely), so we have to emulate a blocking select here.
	    // (Just using Int32.MaxValue isn't good enough because that's only about 35 minutes.)
	    //
	    do {
	    repeatSelect:
		if(checkRead != null)
		{
		    cr = new ArrayList();
		    cr.AddRange(checkRead);
		}
		else
		{
		    cr = null;
		}
		if(checkWrite != null)
		{
		    cw = new ArrayList();
		    cw.AddRange(checkWrite);
		}
		else
		{
		    cw = null;
		}
		if(checkError != null)
		{
		    ce = new ArrayList();
		    ce.AddRange(checkError);
		}
		else
		{
		    ce = null;
		}
		try
		{
		    Socket.Select(cr, cw, ce, System.Int32.MaxValue);
		}
		catch(SocketException e)
		{
		    if(interrupted(e))
		    {
			goto repeatSelect;
		    }
		    throw new Ice.SocketException(e);
		}
	    }
	    while((cr == null || cr.Count == 0) &&
	          (cw == null || cw.Count == 0) &&
		  (ce == null || ce.Count == 0));
	}
	else
	{
	    //
	    // Select() wants microseconds, so we need to deal with overflow.
	    //
	    while(milliSeconds > System.Int32.MaxValue / 1000)
	    {
	    repeatSelect:
		if(checkRead != null)
		{
		    cr = new ArrayList();
		    cr.AddRange(checkRead); 
		}
		else
		{
		    cr = null;
		}
		if(checkWrite != null)
		{
		    cw = new ArrayList();
		    cw.AddRange(checkWrite);
		}
		else
		{
		    cw = null;
		}
		if(checkError != null)
		{
		    ce = new ArrayList();
		    ce.AddRange(checkError);
		}
		else
		{
		    ce = null;
		}
		try
		{
		    Socket.Select(cr, cw, ce, (System.Int32.MaxValue / 1000) * 1000);
		}
		catch(SocketException e)
		{
		    if(interrupted(e))
		    {
		        goto repeatSelect;
		    }
		    throw new Ice.SocketException(e);
		}
		milliSeconds -= System.Int32.MaxValue / 1000;
	    }

	    Socket.Select(checkRead, checkWrite, checkError, milliSeconds * 1000);
	}
    }

    public static IPEndPoint
    getAddress(string host, int port)
    {
	int retry = 5;

    repeatGetHostByName:
	try
	{
	    IPHostEntry e = Dns.GetHostByName(host);
	    Debug.Assert(e.AddressList.Length != 0);
	    return new IPEndPoint(e.AddressList[0], port);
	}
	catch(Win32Exception ex)
	{
	    if(ex.NativeErrorCode == WSATRY_AGAIN && --retry >= 0)
	    {
	        goto repeatGetHostByName;
	    }
	    Ice.DNSException e = new Ice.DNSException("GetHostByName failed", ex);
	    e.host = host;
	    throw e;
	}
	catch(SystemException ex)
	{
	    Ice.DNSException e = new Ice.DNSException("GetHostByName failed", ex);
	    e.host = host;
	    throw e;
	}
    }
    
    public static string
    getLocalHost(bool numeric)
    {
        string hostname;

	int retry = 5;

    repeatGetHostName:
	try
	{
	    hostname = Dns.GetHostName();
	}
	catch(Win32Exception ex)
	{
	    if(ex.NativeErrorCode == WSATRY_AGAIN && --retry >= 0)
	    {
	        goto repeatGetHostName;
	    }
	    Ice.DNSException e = new Ice.DNSException("GetHostName failed", ex);
	    throw e;
	}
	catch(SystemException ex)
	{
	    Ice.DNSException e = new Ice.DNSException("GetHostName failed", ex);
	    throw e;
	}
	
	if(numeric)
	{
	    retry = 5;

	repeatGetHostByName:
	    string numericHost;
	    try
	    {
	        numericHost = Dns.GetHostByName(hostname).AddressList[0].ToString();
	    }
	    catch(Win32Exception ex)
	    {
		if(ex.NativeErrorCode == WSATRY_AGAIN && --retry >= 0)
		{
		    goto repeatGetHostByName;
		}
		Ice.DNSException e = new Ice.DNSException("GetHostByName failed", ex);
		e.host = hostname;
		throw e;
	    }
	    catch(SystemException ex)
	    {
		Ice.DNSException e = new Ice.DNSException("GetHostByName failed", ex);
		e.host = hostname;
		throw e;
	    }
	    hostname = numericHost;
	}

	return hostname;
    }
     
    public sealed class SocketPair
    {
	public Socket source;
	public Socket sink;

	public SocketPair()
	{
	    sink = createSocket(false);
	    Socket listener = createSocket(false);


	    doBind(listener, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));
	    doListen(listener, 1);
	    try
	    {
		doConnect(sink, listener.LocalEndPoint, 1000);
		source = doAccept(listener, -1);
	    }
	    catch(Ice.SocketException ex)
	    {
		try
		{
		    sink.Close();
		}
		catch(SystemException)
		{
		    // ignore
		}
		throw ex;
	    }
	    finally
	    {
		try
		{
		    listener.Close();
		}
		catch(System.Exception)
		{
		}
	    }
	}
    }

    public static SocketPair
    createPipe()
    {
        return new SocketPair();
    }

#if ENDPOINTBUG
    [StructLayout(LayoutKind.Sequential)]
    private struct in_addr
    {
	[MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
	public byte[] sin_addr;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct sockaddr
    {
	public short sin_family;
	public ushort sin_port;
	public in_addr sin_addr;
	[MarshalAs(UnmanagedType.ByValArray, SizeConst=8)]
	public byte[] sin_zero;
    }

    [DllImport("wsock32.dll")]
    private static extern int getsockname(IntPtr s, ref sockaddr name, ref int namelen);

    [DllImport("wsock32.dll")]
    private static extern int getpeername(IntPtr s, ref sockaddr name, ref int namelen);

    [DllImport("ws2_32.dll")]
    private static extern IntPtr inet_ntoa(in_addr a);

    [DllImport("ws2_32.dll")]
    private static extern ushort ntohs(ushort netshort);
#endif

    public static string
    fdToString(Socket socket)
    {
	if(socket == null)
	{
	    return "<closed>";
	}

	//
	// .Net BUG: The LocalEndPoint and RemoteEndPoint properties
	// are null for a socket that was connected in non-blocking
	// mode. The only way to make this work is to step down to
	// the native API and use platform invoke :-(
	//
#if ENDPOINTBUG
	sockaddr addr = new sockaddr();
	int addrLen = 16;

	if(getsockname(socket.Handle, ref addr, ref addrLen) != 0)
	{
	    throw new Ice.SyscallException("getsockname call failed");
	}
	string ip = Marshal.PtrToStringAnsi(inet_ntoa(addr.sin_addr));
	int port = ntohs(addr.sin_port);
	IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);

	IPEndPoint remoteEndpoint = null;
	if(getpeername(socket.Handle, ref addr, ref addrLen) == 0)
	{ 
	    ip = Marshal.PtrToStringAnsi(inet_ntoa(addr.sin_addr));
	    port = ntohs(addr.sin_port);
	    remoteEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
	}
#else
	localEndpoint = (IPEndPoint)socket.LocalEndPoint;

	IPEndPoint remoteEndpoint;
	try
	{
	    remoteEndpoint = (IPEndPoint)socket.RemoteEndPoint;
	}
	catch(SocketException)
	{
	    remoteEndpoint = null;
	}
#endif

	System.Text.StringBuilder s = new System.Text.StringBuilder();
	s.Append("local address = " + localEndpoint.Address);
	s.Append(":" + localEndpoint.Port);

	if(remoteEndpoint == null)
	{
	    s.Append("\nremote address = <not connected>");
	}
	else
	{
	    s.Append("\nremote address = " + IPAddress.Parse(remoteEndpoint.Address.ToString()));
	    s.Append(":" + remoteEndpoint.Port.ToString());
	}
	
	return s.ToString();
    }
    
    public static string
    addrToString(EndPoint addr)
    {
	return addr.ToString();
    }
}

}
