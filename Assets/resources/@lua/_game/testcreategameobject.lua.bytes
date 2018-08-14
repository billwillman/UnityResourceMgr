
local Color = UnityEngine.Color
local GameObject = UnityEngine.GameObject
local ParticleSystem = UnityEngine.ParticleSystem 

function TestCreateGameObject()
	local go = GameObject('go')
	go:AddComponent(typeof(ParticleSystem))
	local node = go.transform
    node.position = Vector3.one                  
    print('gameObject is: '..tostring(go))
end