require "testcreategameobject"

local function TestResoureMgrCreateGameObject()
	local gameObj = Singleton_ResourceMgr.Instance:CreateGameObject("resources/@prefab/cube.prefab")
end

--float process, bool isDone, GameObject gameObj
local function OnTestResourceMgrCreateGameObjectAsync(process, isDone, gameObj)
	if isDone then
		print("CreateGameObjectAsync ok")
	end
end

local function TestResourceMgrCreateGameObjectAsync()
	Singleton_ResourceMgr.Instance:CreateGameObjectAsync("resources/@prefab/cube.prefab", OnTestResourceMgrCreateGameObjectAsync)
end

function Main()
	TestCreateGameObject()
	TestResoureMgrCreateGameObject()
	TestResourceMgrCreateGameObjectAsync()
end