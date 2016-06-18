local complex = {}
complex.real = 0
complex.imag = 0
complex.__index = complex

function Complex(real, imag)
	local new_obj = {}
	setmetatable(new_obj, complex)
	new_obj.real = real or 0
	new_obj.imag = imag or 0
	return new_obj
end

function complex:add(one, two)
	if type(one) ~= "table" and two then
		self.real = self.real + one
		self.imag = self.imag + two
		return
	end
	self.real = self.real + one.real
	self.imag = self.imag + one.imag
end

function complex:addPolar(length, angle)
	angle = math.rad(angle)
	
	self.real = self.real + math.cos(angle) * length
	self.imag = self.imag + math.sin(angle) * length
end

function complex:addReal(amount)
	self.real = self.real + amount
end

function complex:addImag(amount)
	self.imag = self.imag + amount
end

function complex:inverse()
	local div = self.real^2 + self.imag^2
	self.real = self.real / div
	self.imag = -self.imag / div
end

function complex:length()
	return math.sqrt(self.real^2 + self.imag^2)
end

function complex:angle()
	return math.deg(math.atan(self.imag / self.real))
end

function complex:tostring()
	local real = math.round(self.real, 5)
	local imag = math.round(self.imag, 5)
	
	if imag >= 0 then
		return real .." + ".. imag .."i"
	else
		return real .." - ".. -imag .."i"
	end
end

function complex:topolar()
	local length = math.round(self:length(), 5)
	local angle = math.round(self:angle(), 5)
	return length .." < ".. angle .."°"
end