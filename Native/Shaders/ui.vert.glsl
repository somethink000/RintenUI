#version 330 core

// The engine's box shader (addons/base/Assets/shaders/engine/ui.slang), its screen build, in GLSL for OpenGL 3.3.
// Everything a box needs rides on its vertex - see CoreUiRenderer.UiVertex.

layout(location = 0) in vec2 a_position;
layout(location = 1) in vec2 a_uv;
layout(location = 2) in vec4 a_rect;
layout(location = 3) in vec4 a_radii;
layout(location = 4) in vec4 a_fill;
layout(location = 5) in uvec4 a_borders;
layout(location = 6) in vec4 a_misc;
layout(location = 7) in vec4 a_clip;
layout(location = 8) in vec4 a_filter;
layout(location = 9) in vec2 a_filter2;
layout(location = 10) in vec2 a_local;
layout(location = 11) in uint a_texture;
layout(location = 12) in vec4 a_clipRadii;
layout(location = 13) in uvec4 a_vertical;

// xy: the target in pixels
uniform vec4 u_screen;

out vec2 v_screen;
out vec2 v_uv;
out vec4 v_rect;
out vec4 v_radii;
out vec4 v_fill;
flat out uvec4 v_borders;
out vec4 v_misc;
out vec4 v_clip;
out vec4 v_filter;
out vec2 v_filter2;
out vec4 v_clipRadii;
flat out uvec4 v_vertical;

vec3 ToLinear( vec3 c )
{
	return mix( pow( ( c + 0.055 ) / 1.055, vec3( 2.4 ) ), c / 12.92, lessThanEqual( c, vec3( 0.04045 ) ) );
}

void main()
{
	// Pixels from the top left to GL's clip space, whose y points up
	gl_Position = vec4( a_position.x * 2.0 / u_screen.x - 1.0, 1.0 - a_position.y * 2.0 / u_screen.y, 0.0, 1.0 );

	v_screen = a_local;
	v_uv = a_uv;
	v_rect = a_rect;
	v_radii = a_radii;
	v_fill = vec4( ToLinear( a_fill.rgb ), a_fill.a );
	v_borders = a_borders;
	v_misc = a_misc;
	v_clip = a_clip;
	v_filter = a_filter;
	v_filter2 = a_filter2;
	v_clipRadii = a_clipRadii;
	v_vertical = a_vertical;
}
