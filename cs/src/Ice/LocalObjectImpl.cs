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
namespace Ice
{

public abstract class LocalObjectImpl : LocalObject
{
    public virtual int
    ice_hash()
    {
	return GetHashCode();
    }

    public int
    CompareTo(object other)
    {
	if(other == null)
	{
	    return 1;
	}
	if(!(other is Ice.LocalObject))
	{
	    throw new System.ArgumentException("expected object of type Ice.LocalObject", "other");
	}
	int thisHash = GetHashCode();
	int otherHash = other.GetHashCode();
	return thisHash < otherHash ? -1 : (thisHash > otherHash ? 1 : 0);
    }
}

}
