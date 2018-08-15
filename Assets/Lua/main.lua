require "testcreategameobject"

local resMgr = Singleton_ResourceMgr.Instance

local function TestResoureMgrCreateGameObject()
	local gameObj = resMgr:CreateGameObject("resources/@prefab/cube.prefab")
end

--float process, bool isDone, GameObject gameObj
local function OnTestResourceMgrCreateGameObjectAsync(process, isDone, gameObj)
	if isDone then
		print("CreateGameObjectAsync ok")
	end
end

local function TestResourceMgrCreateGameObjectAsync()
	resMgr:CreateGameObjectAsync("resources/@prefab/cube.prefab", OnTestResourceMgrCreateGameObjectAsync)
end

function Main()
	TestCreateGameObject()
	TestResoureMgrCreateGameObject()
	TestResourceMgrCreateGameObjectAsync()
end