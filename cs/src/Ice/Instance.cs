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

namespace IceInternal
{

using System.Diagnostics;

public class Instance
{
    public virtual Ice.Properties
    properties()
    {
	// No mutex lock, immutable.
	return _properties;
    }
    
    public virtual Ice.Logger
    logger()
    {
	lock (this)
	{
	    //
	    // Don't throw CommunicatorDestroyedException if destroyed. We
	    // need the logger also after destructions.
	    //
	    return _logger;
	}
    }
    
    public virtual void
    logger(Ice.Logger logger)
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    _logger = logger;
	}
    }
    
    public virtual Ice.Stats
    stats()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _stats;
	}
    }
    
    public virtual void
    stats(Ice.Stats stats)
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		    throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    _stats = stats;
	}
    }
    
    public virtual TraceLevels
    traceLevels()
    {
	// No mutex lock, immutable.
	return _traceLevels;
    }
    
    public virtual DefaultsAndOverrides
    defaultsAndOverrides()
    {
	// No mutex lock, immutable.
	return _defaultsAndOverrides;
    }
    
    public virtual RouterManager
    routerManager()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _routerManager;
	}
    }
    
    public virtual LocatorManager
    locatorManager()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _locatorManager;
	}
    }
    
    public virtual ReferenceFactory
    referenceFactory()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _referenceFactory;
	}
    }
    
    public virtual ProxyFactory
    proxyFactory()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _proxyFactory;
	}
    }
    
    public virtual OutgoingConnectionFactory
    outgoingConnectionFactory()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _outgoingConnectionFactory;
	}
    }
    
    public virtual ConnectionMonitor
    connectionMonitor()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _connectionMonitor;
	}
    }
    
    public virtual ObjectFactoryManager
    servantFactoryManager()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _servantFactoryManager;
	}
    }
    
    public virtual UserExceptionFactoryManager
    userExceptionFactoryManager()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _userExceptionFactoryManager;
	}
    }
    
    public virtual ObjectAdapterFactory
    objectAdapterFactory()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _objectAdapterFactory;
	}
    }
    
    public virtual ThreadPool
    clientThreadPool()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    if(_clientThreadPool == null)
	    // Lazy initialization.
	    {
		//
		// Make sure that the client thread pool defaults are
		// correctly.
		//
		if(_properties.getProperty("Ice.ThreadPool.Client.Size") == "")
		{
		    _properties.setProperty("Ice.ThreadPool.Client.Size", "1");
		}
		if(_properties.getProperty("Ice.ThreadPool.Client.SizeMax") == "")
		{
		    _properties.setProperty("Ice.ThreadPool.Client.SizeMax", "1");
		}
		if(_properties.getProperty("Ice.ThreadPool.Client.SizeWarn") == "")
		{
		    _properties.setProperty("Ice.ThreadPool.Client.SizeWarn", "0");
		}
		
		_clientThreadPool = new ThreadPool(this, "Ice.ThreadPool.Client", 0);
	    }
	    
	    return _clientThreadPool;
	}
    }
    
    public virtual ThreadPool
    serverThreadPool()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    if(_serverThreadPool == null)
	    // Lazy initialization.
	    {
		int timeout = _properties.getPropertyAsInt("Ice.ServerIdleTime");
		_serverThreadPool = new ThreadPool(this, "Ice.ThreadPool.Server", timeout);
	    }
	    
	    return _serverThreadPool;
	}
    }
    
    public virtual EndpointFactoryManager
    endpointFactoryManager()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _endpointFactoryManager;
	}
    }
    
    public virtual Ice.PluginManager
    pluginManager()
    {
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    return _pluginManager;
	}
    }
    
    public virtual int
    messageSizeMax()
    {
	// No mutex lock, immutable.
	return _messageSizeMax;
    }
    
    public virtual void
    flushBatchRequests()
    {
	OutgoingConnectionFactory connectionFactory;
	ObjectAdapterFactory adapterFactory;
	
	lock (this)
	{
	    if(_destroyed)
	    {
		throw new Ice.CommunicatorDestroyedException();
	    }
	    
	    connectionFactory = _outgoingConnectionFactory;
	    adapterFactory = _objectAdapterFactory;
	}
	
	connectionFactory.flushBatchRequests();
	adapterFactory.flushBatchRequests();
    }
    
    public virtual BufferManager
    bufferManager()
    {
	// No mutex lock, immutable.
	return _bufferManager;
    }
    
    //
    // Only for use by Ice.CommunicatorI
    //
    public
    Instance(Ice.Communicator communicator, ref string[] args, Ice.Properties properties)
    {
	_destroyed = false;
	_properties = properties;
	
	//
	// Convert command-line options to properties.
	//
	args = _properties.parseIceCommandLineOptions(new Ice.StringSeq(args)).ToArray();
	
	try
	{
	    if(_properties.getPropertyAsInt("Ice.UseSyslog") > 0)
	    {
		_logger = new Ice.SysLoggerI(_properties.getProperty("Ice.ProgramName"));
	    }
	    else
	    {
		_logger = new Ice.LoggerI(_properties.getProperty("Ice.ProgramName"), _properties.getPropertyAsInt("Ice.Logger.Timestamp") > 0);
	    }
	    
	    _stats = null; // There is no default statistics callback object.
	    
	    _traceLevels = new TraceLevels(_properties);
	    
	    _defaultsAndOverrides = new DefaultsAndOverrides(_properties);
	    
	    const int defaultMessageSizeMax = 1024;
	    int num = _properties.getPropertyAsIntWithDefault("Ice.MessageSizeMax", defaultMessageSizeMax);
	    if(num < 1)
	    {
		_messageSizeMax = defaultMessageSizeMax * 1024; // Ignore stupid values.
	    }
	    else if(num > 0x7fffffff / 1024)
	    {
		_messageSizeMax = 0x7fffffff;
	    }
	    else
	    {
		_messageSizeMax = num * 1024; // Property is in kilobytes, _messageSizeMax in bytes
	    }
	    
	    _routerManager = new RouterManager();
	    
	    _locatorManager = new LocatorManager();
	    
	    _referenceFactory = new ReferenceFactory(this);
	    
	    _proxyFactory = new ProxyFactory(this);
	    
	    _endpointFactoryManager = new EndpointFactoryManager(this);
	    EndpointFactory tcpEndpointFactory = new TcpEndpointFactory(this);
	    _endpointFactoryManager.add(tcpEndpointFactory);
	    EndpointFactory udpEndpointFactory = new UdpEndpointFactory(this);
	    _endpointFactoryManager.add(udpEndpointFactory);
	    
	    _pluginManager = new Ice.PluginManagerI(communicator);
	    
	    _outgoingConnectionFactory = new OutgoingConnectionFactory(this);
	    
	    _servantFactoryManager = new ObjectFactoryManager();
	    
	    _userExceptionFactoryManager = new UserExceptionFactoryManager();
	    
	    _objectAdapterFactory = new ObjectAdapterFactory(this, communicator);
	    
	    _bufferManager = new BufferManager(); // Must be created before the ThreadPool
	}
	catch(Ice.LocalException ex)
	{
	    destroy();
	    throw ex;
	}
    }
    
    ~Instance()
    {
	Debug.Assert(_destroyed);
	Debug.Assert(_referenceFactory == null);
	Debug.Assert(_proxyFactory == null);
	Debug.Assert(_outgoingConnectionFactory == null);
	Debug.Assert(_connectionMonitor == null);
	Debug.Assert(_servantFactoryManager == null);
	Debug.Assert(_userExceptionFactoryManager == null);
	Debug.Assert(_objectAdapterFactory == null);
	Debug.Assert(_clientThreadPool == null);
	Debug.Assert(_serverThreadPool == null);
	Debug.Assert(_routerManager == null);
	Debug.Assert(_locatorManager == null);
	Debug.Assert(_endpointFactoryManager == null);
	Debug.Assert(_pluginManager == null);
    }
    
    public virtual void
    finishSetup(ref string[] args)
    {
	//
	// Load plug-ins.
	//
	Ice.PluginManagerI pluginManagerImpl = (Ice.PluginManagerI)_pluginManager;
	pluginManagerImpl.loadPlugins(ref args);
	
	//
	// Get default router and locator proxies. Don't move this
	// initialization before the plug-in initialization!!! The proxies
	// might depend on endpoint factories to be installed by plug-ins.
	//
	if(_defaultsAndOverrides.defaultRouter.Length > 0)
	{
	    _referenceFactory.setDefaultRouter(Ice.RouterPrxHelper._uncheckedCast(_proxyFactory.stringToProxy(_defaultsAndOverrides.defaultRouter)));
	}
	
	if(_defaultsAndOverrides.defaultLocator.Length > 0)
	{
	    _referenceFactory.setDefaultLocator(Ice.LocatorPrxHelper._uncheckedCast(_proxyFactory.stringToProxy(_defaultsAndOverrides.defaultLocator)));
	}
	
	//
	// Connection monitor initializations must be done after
	// daemon() is called, since daemon() forks.
	//
	int acmTimeout = _properties.getPropertyAsInt("Ice.ConnectionIdleTime");
	int interval = _properties.getPropertyAsIntWithDefault("Ice.MonitorConnections", acmTimeout);
	if(interval > 0)
	{
	    _connectionMonitor = new ConnectionMonitor(this, interval);
	}
	
	//
	// Thread pool initialization is now lazy initialization in
	// clientThreadPool() and serverThreadPool().
	//
    }
    
    //
    // Only for use by Ice.CommunicatorI
    //
    public virtual void
    destroy()
    {
	Debug.Assert(!_destroyed);
	
	if(_objectAdapterFactory != null)
	{
	    _objectAdapterFactory.shutdown();
	}
	
	if(_outgoingConnectionFactory != null)
	{
	    _outgoingConnectionFactory.destroy();
	}
	
	if(_objectAdapterFactory != null)
	{
	    _objectAdapterFactory.waitForShutdown();
	}
	
	if(_outgoingConnectionFactory != null)
	{
	    _outgoingConnectionFactory.waitUntilFinished();
	}
	
	ThreadPool serverThreadPool = null;
	ThreadPool clientThreadPool = null;
	
	lock (this)
	{
	    _objectAdapterFactory = null;
	    
	    _outgoingConnectionFactory = null;
	    
	    if(_connectionMonitor != null)
	    {
		//UPGRADE_ISSUE: Method 'java.lang.Thread.destroy' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javalangThreaddestroy"'
		_connectionMonitor.destroy();
		_connectionMonitor = null;
	    }
	    
	    if(_serverThreadPool != null)
	    {
		_serverThreadPool.destroy();
		serverThreadPool = _serverThreadPool;
		_serverThreadPool = null;
	    }
	    
	    if(_clientThreadPool != null)
	    {
		_clientThreadPool.destroy();
		clientThreadPool = _clientThreadPool;
		_clientThreadPool = null;
	    }
	    
	    if(_servantFactoryManager != null)
	    {
		_servantFactoryManager.destroy();
		_servantFactoryManager = null;
	    }
	    
	    if(_userExceptionFactoryManager != null)
	    {
		_userExceptionFactoryManager.destroy();
		_userExceptionFactoryManager = null;
	    }
	    
	    if(_referenceFactory != null)
	    {
		_referenceFactory.destroy();
		_referenceFactory = null;
	    }
	    
	    // No destroy function defined.
	    // _proxyFactory.destroy();
	    _proxyFactory = null;
	    
	    if(_routerManager != null)
	    {
		_routerManager.destroy();
		_routerManager = null;
	    }
	    
	    if(_locatorManager != null)
	    {
		_locatorManager.destroy();
		_locatorManager = null;
	    }
	    
	    if(_endpointFactoryManager != null)
	    {
		_endpointFactoryManager.destroy();
		_endpointFactoryManager = null;
	    }
	    
	    if(_pluginManager != null)
	    {
		_pluginManager.destroy();
		_pluginManager = null;
	    }
	    
	    _destroyed = true;
	}
	
	//
	// Join with the thread pool threads outside the
	// synchronization.
	//
	if(clientThreadPool != null)
	{
	    clientThreadPool.joinWithAllThreads();
	}
	if(serverThreadPool != null)
	{
	    serverThreadPool.joinWithAllThreads();
	}
    }
    
    private bool _destroyed;
    private volatile Ice.Properties _properties;
    // Immutable, not reset by destroy().
    private Ice.Logger _logger; // Not reset by destroy().
    private Ice.Stats _stats; // Not reset by destroy().
    private volatile TraceLevels _traceLevels; // Immutable, not reset by destroy().
    private volatile DefaultsAndOverrides _defaultsAndOverrides; // Immutable, not reset by destroy().
    private volatile int _messageSizeMax; // Immutable, not reset by destroy().
    private RouterManager _routerManager;
    private LocatorManager _locatorManager;
    private ReferenceFactory _referenceFactory;
    private ProxyFactory _proxyFactory;
    private OutgoingConnectionFactory _outgoingConnectionFactory;
    private ConnectionMonitor _connectionMonitor;
    private ObjectFactoryManager _servantFactoryManager;
    private UserExceptionFactoryManager _userExceptionFactoryManager;
    private ObjectAdapterFactory _objectAdapterFactory;
    private ThreadPool _clientThreadPool;
    private ThreadPool _serverThreadPool;
    private EndpointFactoryManager _endpointFactoryManager;
    private Ice.PluginManager _pluginManager;
    private volatile BufferManager _bufferManager; // Immutable, not reset by destroy().
}

}
