// **********************************************************************
//
// Copyright (c) 2003-2006 ZeroC, Inc. All rights reserved.
//
// This copy of Ice is licensed to you under the terms described in the
// ICE_LICENSE file included in this distribution.
//
// **********************************************************************

#include <IceUtil/UUID.h>

#include <Ice/Ice.h>
#include <Ice/LoggerUtil.h>
#include <Ice/TraceUtil.h>
#include <Ice/SliceChecksums.h>

#include <IceGrid/AdminI.h>
#include <IceGrid/RegistryI.h>
#include <IceGrid/Database.h>
#include <IceGrid/Util.h>
#include <IceGrid/DescriptorParser.h>
#include <IceGrid/DescriptorHelper.h>
#include <IceGrid/AdminSessionI.h>

using namespace std;
using namespace Ice;
using namespace IceGrid;

namespace IceGrid
{

class ServerProxyWrapper
{
public:

    ServerProxyWrapper(const DatabasePtr& database, const string& id) : _id(id)
    {
	_proxy = database->getServer(_id)->getProxy(_activationTimeout, _deactivationTimeout, _node);
    }
    
    void
    useActivationTimeout()
    {
	_proxy = ServerPrx::uncheckedCast(_proxy->ice_timeout(_activationTimeout * 1000));
    }

    void
    useDeactivationTimeout()
    {
	_proxy = ServerPrx::uncheckedCast(_proxy->ice_timeout(_deactivationTimeout * 1000));
    }

    IceProxy::IceGrid::Server* 
    operator->() const
    {
	return _proxy.get();
    }

    void
    handleException(const Ice::Exception& ex)
    {
	try
	{
	    ex.ice_throw();
	}
	catch(const Ice::UserException&)
	{
	    throw;
	}
	catch(const Ice::ObjectNotExistException&)
	{
	    throw ServerNotExistException(_id);
	}
	catch(const Ice::LocalException& e)
	{
	    ostringstream os;
	    os << e;
	    throw NodeUnreachableException(_node, os.str());
	}
    }

private:

    string _id;
    ServerPrx _proxy;
    int _activationTimeout;
    int _deactivationTimeout;
    string _node;
};

template<class AmdCB>
class PatcherFeedbackI : public PatcherFeedback, public IceUtil::Mutex
{
public:

    PatcherFeedbackI(const AmdCB& cb,
		     const AdminIPtr& admin,
		     Ice::Identity id,
		     const TraceLevelsPtr& traceLevels,
		     const string& type,
		     const string& name,
		     int nodeCount) : 
	_cb(cb), 
	_admin(admin),
	_id(id),
	_traceLevels(traceLevels),
	_type(type),
	_name(name),
	_count(nodeCount),
	_nSuccess(0), 
	_nFailure(0)
    {
    }

    ~PatcherFeedbackI()
    {
	Lock sync(*this);
	if((_nSuccess + _nFailure) < _count)
	{
	    PatchException ex;
	    ex.reasons.push_back("admin session destroyed");
	    _cb->ice_exception(ex);
	}
    }

    void
    finished(const string& node, const Ice::Current&)
    {
	Lock sync(*this);
	if(_traceLevels->patch > 0)
	{
	    Ice::Trace out(_traceLevels->logger, _traceLevels->patchCat);
	    out << "finished patching of " << _type << " `" << _name << "' on node `" << node << "'";
	}
	++_nSuccess;
	checkIfDone();
    }

    void
    failed(const string& node, const string& failure, const Ice::Current& = Ice::Current())
    {
	Lock sync(*this);
	if(_traceLevels->patch > 0)
	{
	    Ice::Trace out(_traceLevels->logger, _traceLevels->patchCat);
	    out << "patching of " << _type << " `" << _name << "' on node `" << node <<"' failed:\n" << failure;
	}
	
	++_nFailure;
	_reasons.push_back("patch on node `" + node + "' failed:\n" + failure);
	checkIfDone();
    }

    void
    failed(const string& node, const Ice::Exception& ex)
    {
	try
	{
	    ex.ice_throw();
	}
	catch(const NodeNotExistException&)
	{
	    failed(node, "node doesn't exist");
	}
	catch(const NodeUnreachableException& e)
	{
	    failed(node, "node is unreachable: " + e.reason);
	}
	catch(const Ice::Exception& e)
	{
	    ostringstream os;
	    os << e;
	    failed(node, "node is unreachable:\n" + os.str());
	}
    }

    void
    checkIfDone()
    {
	if((_nSuccess + _nFailure) == _count)
	{
	    if(_nFailure)
	    {
		sort(_reasons.begin(), _reasons.end());
		PatchException ex;
		ex.reasons = _reasons;
		_cb->ice_exception(ex);
	    }
	    else
	    {
		_cb->ice_response();
	    }

	    _admin->removeFeedbackIdentity(_id);
	}
    }

private:

    const AmdCB _cb;
    const AdminIPtr _admin;
    const Ice::Identity _id;
    const TraceLevelsPtr _traceLevels;
    const string _type;
    const string _name;
    const int _count;
    int _nSuccess;
    int _nFailure;
    Ice::StringSeq _reasons;    
};

}

AdminI::AdminI(const DatabasePtr& database, const RegistryIPtr& registry, const AdminSessionIPtr& session) :
    _database(database),
    _registry(registry),
    _traceLevels(_database->getTraceLevels()),
    _session(session)
{
}

AdminI::~AdminI()
{
    try
    {
	for(set<Ice::Identity>::const_iterator p = _feedbackIdentities.begin(); p != _feedbackIdentities.end(); ++p)
	{
	    try
	    {
		_database->getInternalAdapter()->remove(*p);
	    }
	    catch(Ice::NotRegisteredException&)
	    {
	    }
	}
    }
    catch(const Ice::LocalException& ex)
    {
    }
}


void
AdminI::addApplication(const ApplicationDescriptor& descriptor, const Current&)
{
    checkIsMaster();

    ApplicationInfo info;
    info.createTime = info.updateTime = IceUtil::Time::now().toMilliSeconds();
    info.createUser = info.updateUser = _session->getId();
    info.descriptor = descriptor;
    info.revision = 1;
    info.uuid = IceUtil::generateUUID();

    _database->addApplication(info, _session.get());
}

void
AdminI::syncApplication(const ApplicationDescriptor& descriptor, const Current&)
{
    checkIsMaster();
    _database->syncApplicationDescriptor(descriptor, _session.get());
}

void
AdminI::updateApplication(const ApplicationUpdateDescriptor& descriptor, const Current&)
{
    checkIsMaster();

    ApplicationUpdateInfo update;
    update.updateTime = IceUtil::Time::now().toMilliSeconds();
    update.updateUser = _session->getId();
    update.descriptor = descriptor;
    update.revision = -1; // The database will set it.
    _database->updateApplication(update, _session.get());
}

void
AdminI::removeApplication(const string& name, const Current&)
{
    checkIsMaster();
    _database->removeApplication(name, _session.get());
}

void
AdminI::instantiateServer(const string& app, const string& node, const ServerInstanceDescriptor& desc, const Current&)
{
    checkIsMaster();
    _database->instantiateServer(app, node, desc, _session.get());
}

void
AdminI::patchApplication_async(const AMD_Admin_patchApplicationPtr& amdCB, 
			       const string& name, 
			       bool shutdown, 
			       const Current& current)
{
    ApplicationHelper helper(current.adapter->getCommunicator(), _database->getApplicationInfo(name).descriptor);
    DistributionDescriptor appDistrib;
    vector<string> nodes;
    helper.getDistributions(appDistrib, nodes);

    if(nodes.empty())
    {
	amdCB->ice_response();
	return;
    }

    Ice::Identity id;
    id.category = current.id.category;
    id.name = IceUtil::generateUUID();

    PatcherFeedbackI<AMD_Admin_patchApplicationPtr>* feedback = 
	new PatcherFeedbackI<AMD_Admin_patchApplicationPtr>(amdCB, this, id, _traceLevels, "application", name,
							    static_cast<int>(nodes.size()));

    PatcherFeedbackPtr servant = feedback;
    PatcherFeedbackPrx prx = PatcherFeedbackPrx::uncheckedCast(_database->getInternalAdapter()->add(servant, id));
    {
	Lock sync(*this);
	_feedbackIdentities.insert(prx->ice_getIdentity());
    }

    for(vector<string>::const_iterator p = nodes.begin(); p != nodes.end(); ++p)
    {
	try
	{
	    if(_traceLevels->patch > 0)
	    {
		Ice::Trace out(_traceLevels->logger, _traceLevels->patchCat);
		out << "started patching of application `" << name << "' on node `" << *p << "'";
	    }

	    NodeEntryPtr node = _database->getNode(*p);
	    Resolver resolve(node->getInfo(), _database->getCommunicator());
	    node->getProxy()->patch(prx, name, "", resolve(appDistrib), shutdown);
	}
	catch(const Ice::Exception& ex)
	{
	    feedback->failed(*p, ex);
	}
    }
}

ApplicationInfo
AdminI::getApplicationInfo(const string& name, const Current&) const
{
    return _database->getApplicationInfo(name);
}

ApplicationDescriptor
AdminI::getDefaultApplicationDescriptor(const Current& current) const
{
    Ice::PropertiesPtr properties = current.adapter->getCommunicator()->getProperties();
    string path = properties->getProperty("IceGrid.Registry.DefaultTemplates");
    if(path.empty())
    {
	throw DeploymentException("no default templates configured, you need to set "
				  "IceGrid.Registry.DefaultTemplates in the IceGrid registry configuration.");
    }

    ApplicationDescriptor desc;
    try
    {
	desc = DescriptorParser::parseDescriptor(path, current.adapter->getCommunicator());
    }
    catch(const IceXML::ParserException& ex)
    {
	throw DeploymentException("can't parse default templates:\n" + ex.reason());
    }
    desc.name = "";
    if(!desc.nodes.empty())
    {
	Ice::Warning warn(_traceLevels->logger);
	warn << "default application descriptor:\nnode definitions are not allowed.";
	desc.nodes.clear();
    }
    if(!desc.distrib.icepatch.empty() || !desc.distrib.directories.empty())
    {
	Ice::Warning warn(_traceLevels->logger);
	warn << "default application descriptor:\ndistribution is not allowed.";
	desc.distrib = DistributionDescriptor();
    }
    if(!desc.replicaGroups.empty())
    {
	Ice::Warning warn(_traceLevels->logger);
	warn << "default application descriptor:\nreplica group definitions are not allowed.";
	desc.replicaGroups.clear();
    }
    if(!desc.description.empty())
    {
	Ice::Warning warn(_traceLevels->logger);
	warn << "default application descriptor:\ndescription is not allowed.";
	desc.description = "";
    }
    if(!desc.variables.empty())
    {
	Ice::Warning warn(_traceLevels->logger);
	warn << "default application descriptor:\nvariable definitions are not allowed.";
	desc.variables.clear();
    }
    return desc;
}

Ice::StringSeq
AdminI::getAllApplicationNames(const Current&) const
{
    return _database->getAllApplications();
}

ServerInfo
AdminI::getServerInfo(const string& id, const Current&) const
{
    return _database->getServer(id)->getInfo(true);
}

ServerState
AdminI::getServerState(const string& id, const Current&) const
{
    ServerProxyWrapper proxy(_database, id);
    try
    {
	return proxy->getState();
    }
    catch(const Ice::Exception& ex)
    {
	proxy.handleException(ex);
	return Inactive;
    }
}

Ice::Int
AdminI::getServerPid(const string& id, const Current&) const
{
    ServerProxyWrapper proxy(_database, id);
    try
    {
	return proxy->getPid();
    }
    catch(const Ice::Exception& ex)
    {
	proxy.handleException(ex);
	return 0;
    }
}

void
AdminI::startServer(const string& id, const Current&)
{
    ServerProxyWrapper proxy(_database, id);
    proxy.useActivationTimeout();
    try
    {
	proxy->start();
    }
    catch(const Ice::Exception& ex)
    {
	proxy.handleException(ex);
    }
}

void
AdminI::stopServer(const string& id, const Current&)
{
    ServerProxyWrapper proxy(_database, id);
    proxy.useDeactivationTimeout();
    try
    {
	proxy->stop();
    }
    catch(const Ice::TimeoutException&)
    {
    }
    catch(const Ice::Exception& ex)
    {
	proxy.handleException(ex);
    }
}

void
AdminI::patchServer_async(const AMD_Admin_patchServerPtr& amdCB, const string& id, bool shutdown,
			  const Current& current)
{
    ServerInfo info = _database->getServer(id)->getInfo();
    ApplicationInfo appInfo = _database->getApplicationInfo(info.application);
    ApplicationHelper helper(current.adapter->getCommunicator(), appInfo.descriptor);
    DistributionDescriptor appDistrib;
    vector<string> nodes;
    helper.getDistributions(appDistrib, nodes, id);

    if(appDistrib.icepatch.empty() && nodes.empty())
    {
	amdCB->ice_response();
	return;
    }

    assert(nodes.size() == 1);

    Ice::Identity identity;
    identity.category = current.id.category;
    identity.name = IceUtil::generateUUID();

    PatcherFeedbackI<AMD_Admin_patchServerPtr>* feedback = 
	new PatcherFeedbackI<AMD_Admin_patchServerPtr>(amdCB, this, identity, _traceLevels, "server", id,
						       static_cast<int>(nodes.size()));

    PatcherFeedbackPtr servant = feedback;
    PatcherFeedbackPrx prx = PatcherFeedbackPrx::uncheckedCast(_database->getInternalAdapter()->add(servant, 
												    identity));
    {
	Lock sync(*this);
	_feedbackIdentities.insert(prx->ice_getIdentity());
    }

    vector<string>::const_iterator p = nodes.begin();
    try
    {
	if(_traceLevels->patch > 0)
	{
	    Ice::Trace out(_traceLevels->logger, _traceLevels->patchCat);
	    out << "started patching of server `" << id << "' on node `" << *p << "'";
	}

	NodeEntryPtr node = _database->getNode(*p);
	Resolver resolve(node->getInfo(), _database->getCommunicator());
	node->getProxy()->patch(prx, info.application, id, resolve(appDistrib), shutdown);
    }
    catch(const Ice::Exception& ex)
    {
	feedback->failed(*p, ex);
    }
}

void
AdminI::sendSignal(const string& id, const string& signal, const Current&)
{
    ServerProxyWrapper proxy(_database, id);
    try
    {
	proxy->sendSignal(signal);
    }
    catch(const Ice::Exception& ex)
    {
	proxy.handleException(ex);
    }
}

void
AdminI::writeMessage(const string& id, const string& message, Int fd, const Current&)
{
    ServerProxyWrapper proxy(_database, id);
    try
    {
	proxy->writeMessage(message, fd);
    }
    catch(const Ice::Exception& ex)
    {
	proxy.handleException(ex);
    }
}

StringSeq
AdminI::getAllServerIds(const Current&) const
{
    return _database->getServerCache().getAll("");
}

void 
AdminI::enableServer(const string& id, bool enable, const Ice::Current&)
{
    ServerProxyWrapper proxy(_database, id);
    try
    {
	proxy->setEnabled(enable);
    }
    catch(const Ice::Exception& ex)
    {
	proxy.handleException(ex);
    }
}

bool
AdminI::isServerEnabled(const ::std::string& id, const Ice::Current&) const
{
    ServerProxyWrapper proxy(_database, id);
    try
    {
	return proxy->isEnabled();
    }
    catch(const Ice::Exception& ex)
    {
	proxy.handleException(ex);
	return true; // Keeps the compiler happy.
    }
}

AdapterInfoSeq
AdminI::getAdapterInfo(const string& id, const Current&) const
{
    return _database->getAdapterInfo(id);
}

void
AdminI::removeAdapter(const string& adapterId, const Ice::Current&)
{
    checkIsMaster();
    _database->removeAdapter(adapterId);
}

StringSeq
AdminI::getAllAdapterIds(const Current&) const
{
    return _database->getAllAdapters();
}

void 
AdminI::addObject(const Ice::ObjectPrx& proxy, const ::Ice::Current& current)
{
    checkIsMaster();
    try
    {
	addObjectWithType(proxy, proxy->ice_id(), current);
    }
    catch(const Ice::LocalException& e)
    {
	ostringstream os;

	os << "failed to invoke ice_id() on proxy `" + current.adapter->getCommunicator()->proxyToString(proxy);
	os << "':\n" << e;
	throw DeploymentException(os.str());
    }
}

void 
AdminI::updateObject(const Ice::ObjectPrx& proxy, const ::Ice::Current& current)
{
    checkIsMaster();
    const Ice::Identity id = proxy->ice_getIdentity();
    if(id.category == _database->getInstanceName())
    {
	DeploymentException ex;
	ex.reason ="updating object `" + _database->getCommunicator()->identityToString(id) + "' is not allowed";
	throw ex;
    }
    _database->updateObject(proxy);
}

void 
AdminI::addObjectWithType(const Ice::ObjectPrx& proxy, const string& type, const ::Ice::Current& current)
{
    checkIsMaster();
    const Ice::Identity id = proxy->ice_getIdentity();
    if(id.category == _database->getInstanceName())
    {
	DeploymentException ex;
	ex.reason = "adding object `" + _database->getCommunicator()->identityToString(id) + "' is not allowed";
	throw ex;
    }

    ObjectInfo info;
    info.proxy = proxy;
    info.type = type;
    _database->addObject(info);
}

void 
AdminI::removeObject(const Ice::Identity& id, const Ice::Current& current)
{
    checkIsMaster();
    if(id.category == _database->getInstanceName())
    {
	DeploymentException ex;
	ex.reason = "removing object `" + _database->getCommunicator()->identityToString(id) + "' is not allowed";
	throw ex;
    }
    _database->removeObject(id);
}

ObjectInfo
AdminI::getObjectInfo(const Ice::Identity& id, const Ice::Current&) const
{
    return _database->getObjectInfo(id);
}

ObjectInfoSeq
AdminI::getObjectInfosByType(const string& type, const Ice::Current&) const
{
    return _database->getObjectInfosByType(type);
}

ObjectInfoSeq
AdminI::getAllObjectInfos(const string& expression, const Ice::Current&) const
{
    return _database->getAllObjectInfos(expression);
}

NodeInfo
AdminI::getNodeInfo(const string& name, const Ice::Current&) const
{
    return _database->getNode(name)->getInfo();
}

bool
AdminI::pingNode(const string& name, const Current&) const
{
    try
    {
	_database->getNode(name)->getProxy()->ice_ping();
	return true;
    }
    catch(const NodeUnreachableException&)
    {
	return false;
    }
    catch(const Ice::ObjectNotExistException&)
    {
	throw NodeNotExistException();
    }
    catch(const Ice::LocalException&)
    {
	return false;
    }
}

LoadInfo
AdminI::getNodeLoad(const string& name, const Current&) const
{
    try
    {
	return _database->getNode(name)->getProxy()->getLoad();
    }
    catch(const Ice::ObjectNotExistException&)
    {
	throw NodeNotExistException();
    }
    catch(const Ice::LocalException& ex)
    {
	ostringstream os;
	os << ex;
	throw NodeUnreachableException(name, os.str());
    }    
    return LoadInfo(); // Keep the compiler happy.
}

void
AdminI::shutdownNode(const string& name, const Current&)
{
    try
    {
	_database->getNode(name)->getProxy()->shutdown();
    }
    catch(const Ice::ObjectNotExistException&)
    {
	throw NodeNotExistException(name);
    }
    catch(const Ice::LocalException& ex)
    {
	ostringstream os;
	os << ex;
	throw NodeUnreachableException(name, os.str());
    }
}

string
AdminI::getNodeHostname(const string& name, const Current&) const
{
    try
    {
	return _database->getNode(name)->getInfo().hostname;
    }
    catch(const Ice::ObjectNotExistException&)
    {
	throw NodeNotExistException(name);
    }
    catch(const Ice::LocalException& ex)
    {
	ostringstream os;
	os << ex;
	throw NodeUnreachableException(name, os.str());
	return ""; // Keep the compiler happy.
    }
}


StringSeq
AdminI::getAllNodeNames(const Current&) const
{
    return _database->getNodeCache().getAll("");
}

RegistryInfo
AdminI::getRegistryInfo(const string& name, const Ice::Current&) const
{
    if(name == _registry->getName())
    {
	return _registry->getInfo();
    }
    else
    {
	return _database->getReplica(name)->getInfo();
    }
}

bool
AdminI::pingRegistry(const string& name, const Current&) const
{
    if(name == _registry->getName())
    {
 	return true;
    }

    try
    {
	_database->getReplica(name)->getProxy()->ice_ping();
	return true;
    }
    catch(const Ice::ObjectNotExistException&)
    {
	throw RegistryNotExistException();
    }
    catch(const Ice::LocalException&)
    {
	return false;
    }
    return false;
}

void
AdminI::shutdownRegistry(const string& name, const Current&)
{
    if(name == _registry->getName())
    {
	_registry->shutdown();
	return;
    }

    try
    {
	_database->getReplica(name)->getProxy()->shutdown();
    }
    catch(const Ice::ObjectNotExistException&)
    {
	throw RegistryNotExistException(name);
    }
    catch(const Ice::LocalException& ex)
    {
	ostringstream os;
	os << ex;
	throw RegistryUnreachableException(name, os.str());
    }
}

StringSeq
AdminI::getAllRegistryNames(const Current&) const
{
    Ice::StringSeq replicas = _database->getReplicaCache().getAll("");
    replicas.push_back(_registry->getName());
    return replicas;
}

void
AdminI::shutdown(const Current&)
{
    _registry->shutdown();
}

SliceChecksumDict
AdminI::getSliceChecksums(const Current&) const
{
    return sliceChecksums();
}

void
AdminI::removeFeedbackIdentity(const Ice::Identity& id)
{
    Lock sync(*this);
    _feedbackIdentities.erase(id);
    
    try
    {
	_database->getInternalAdapter()->remove(id);
    }
    catch(const Ice::LocalException&)
    {
    }
}

void
AdminI::checkIsMaster() const
{
    if(!_database->isMaster())
    {
	DeploymentException ex;
	ex.reason = "this operation is only allowed on the master registry.";
	throw ex;
    }
}

