#version 330 core

// The engine's box shader (addons/base/Assets/shaders/engine/ui.slang), its screen build, in GLSL for OpenGL 3.3: one signed-distance
// rounded rectangle per quad, the border drawn as the ring between the outer edge and the same edge pushed inward. The comments that
// say why are the original's, shortened; the backdrop filter is left out, as an app's window draws no copy of itself to blur.

in vec2 v_screen;
in vec2 v_uv;
in vec4 v_rect;
in vec4 v_radii;
in vec4 v_fill;
flat in uvec4 v_borders;
in vec4 v_misc;
in vec4 v_clip;
in vec4 v_filter;
in vec2 v_filter2;
in vec4 v_clipRadii;
flat in uvec4 v_vertical;

uniform vec4 u_screen;
uniform sampler2D u_color;

out vec4 o_colour;

vec3 ToLinear( vec3 c )
{
	return mix( pow( ( c + 0.055 ) / 1.055, vec3( 2.4 ) ), c / 12.92, lessThanEqual( c, vec3( 0.04045 ) ) );
}

vec3 ToGamma( vec3 c )
{
	c = max( c, 0.0 );
	return mix( 1.055 * pow( c, vec3( 1.0 / 2.4 ) ) - 0.055, c * 12.92, lessThanEqual( c, vec3( 0.0031308 ) ) );
}

float ToSrgb( float light )
{
	return light <= 0.0031308 ? light * 12.92 : 1.055 * pow( light, 1.0 / 2.4 ) - 0.055;
}

float HalfToFloat( uint bits )
{
	uint exponent = ( bits >> 10 ) & 0x1fu;
	uint mantissa = bits & 0x3ffu;

	float value = exponent == 0u
		? float( mantissa ) * 5.9604644775390625e-8
		: uintBitsToFloat( ( ( exponent + 112u ) << 23 ) | ( mantissa << 13 ) );

	return ( bits & 0x8000u ) != 0u ? -value : value;
}

// UV for image-rendering: pixelated: texel centre, crossing only in the last pixel before an edge
vec2 PixelatedUv( vec2 uv )
{
	vec2 size = max( vec2( textureSize( u_color, 0 ) ), 1.0 );
	vec2 texel = uv * size;
	vec2 across = max( fwidth( texel ), 1e-5 );
	vec2 fromMiddle = texel - 0.5;

	return ( floor( fromMiddle ) + 0.5 + clamp( ( fract( fromMiddle ) - 0.5 ) / across + 0.5, 0.0, 1.0 ) ) / size;
}

// Four vertical radii out of two words of half floats, where nought means the corner's horizontal radius - a circle
vec4 VerticalRadii( vec4 horizontal, uvec2 words )
{
	vec4 v = vec4(
		HalfToFloat( words.x & 0xffffu ), HalfToFloat( words.x >> 16 ),
		HalfToFloat( words.y & 0xffffu ), HalfToFloat( words.y >> 16 ) );

	vec4 given = step( 0.0001, v );
	return v * given + horizontal * ( 1.0 - given );
}

vec4 Unpacked( uint colour )
{
	return vec4(
		float( colour & 0xffu ), float( ( colour >> 8 ) & 0xffu ),
		float( ( colour >> 16 ) & 0xffu ), float( colour >> 24 ) ) / 255.0;
}

vec2 CornerOf( vec2 p, vec4 h, vec4 v )
{
	return p.x < 0.0
		? ( p.y < 0.0 ? vec2( h.x, v.x ) : vec2( h.w, v.w ) )
		: ( p.y < 0.0 ? vec2( h.y, v.y ) : vec2( h.z, v.z ) );
}

vec2 HeldCorner( vec2 r, vec2 halfSize )
{
	return r.x == r.y
		? vec2( min( r.x, min( halfSize.x, halfSize.y ) ) )
		: min( r, halfSize );
}

float RoundedBox( vec2 p, vec2 halfSize, vec4 radii, vec4 radiiV )
{
	vec2 r = HeldCorner( CornerOf( p, radii, radiiV ), halfSize );

	if ( r.x == r.y )
	{
		vec2 q = abs( p ) - halfSize + r.x;
		return length( max( q, 0.0 ) ) + min( max( q.x, q.y ), 0.0 ) - r.x;
	}

	// A quarter of an ellipse: the usual first-order distance, exact on the curve
	vec2 d = abs( p ) - halfSize;
	vec2 q = d + r;

	if ( q.x > 0.0 && q.y > 0.0 && min( r.x, r.y ) > 0.0001 )
	{
		float k0 = length( q / r );
		float k1 = length( q / ( r * r ) );
		return k0 * ( k0 - 1.0 ) / max( k1, 0.000001 );
	}

	return length( max( d, 0.0 ) ) + min( max( d.x, d.y ), 0.0 );
}

vec2 InnerCorner( float h, float v, float sideX, float sideY )
{
	if ( h == v ) return vec2( max( h - max( sideX, sideY ), 0.0 ) );

	vec2 inner = vec2( h - sideX, v - sideY );
	return inner.x > 0.0 && inner.y > 0.0 ? inner : vec2( 0.0 );
}

const int BorderDotted = 3;
const int BorderDashed = 4;

struct BorderContour
{
	float along;
	float len;
	float across;
	vec2 normal;
};

float EllipseLength( vec2 radii, float angle )
{
	if ( radii.x == radii.y ) return radii.x * angle;

	const vec4 nodes = vec4( 0.1834346425, 0.5255324099, 0.7966664774, 0.9602898565 );
	const vec4 weights = vec4( 0.3626837834, 0.3137066459, 0.2223810345, 0.1012285363 );

	float result = 0.0;

	for ( int i = 0; i < 4; i++ )
	{
		vec2 t = angle * 0.5 * vec2( 1.0 - nodes[i], 1.0 + nodes[i] );
		vec2 s = sin( t );
		vec2 c = cos( t );

		result += weights[i] * ( length( radii * vec2( s.x, c.x ) ) + length( radii * vec2( s.y, c.y ) ) );
	}

	return result * angle * 0.5;
}

vec2 Normalised( vec2 v )
{
	float size = length( v );
	return size > 0.000001 ? v / size : vec2( 1.0, 0.0 );
}

BorderContour Contour( vec2 p, vec2 size, vec4 widths, vec4 h, vec4 v )
{
	vec4 inset = widths * 0.5;
	vec2 inner = max( size - inset.xy - inset.zw, 0.0 );
	p -= inset.xy;

	h = max( h - inset.xzzx, 0.0 );
	v = max( v - inset.yyww, 0.0 );

	vec4 keep = step( 0.0001, h ) * step( 0.0001, v );
	h *= keep;
	v *= keep;

	vec2 starts[4] = vec2[4]( vec2( h.x, 0.0 ), vec2( inner.x, v.y ), vec2( inner.x - h.z, inner.y ), vec2( 0.0, inner.y - v.w ) );
	vec2 ends[4] = vec2[4]( vec2( inner.x - h.y, 0.0 ), vec2( inner.x, inner.y - v.z ), vec2( h.w, inner.y ), vec2( 0.0, v.x ) );
	vec2 centres[4] = vec2[4]( vec2( inner.x - h.y, v.y ), vec2( inner.x - h.z, inner.y - v.z ), vec2( h.w, inner.y - v.w ), vec2( h.x, v.x ) );
	vec2 axes[4] = vec2[4]( vec2( 0.0, -1.0 ), vec2( 1.0, 0.0 ), vec2( 0.0, 1.0 ), vec2( -1.0, 0.0 ) );
	vec2 radii[4] = vec2[4]( vec2( v.y, h.y ), vec2( h.z, v.z ), vec2( v.w, h.w ), vec2( h.x, v.x ) );

	BorderContour result;
	result.along = 0.0;
	result.len = 0.0;
	result.across = 0.0;
	result.normal = vec2( 0.0, -1.0 );

	float nearest = 1e30;

	for ( int i = 0; i < 4; i++ )
	{
		vec2 delta = ends[i] - starts[i];
		float extent = length( delta );
		float t = clamp( dot( p - starts[i], delta ) / max( dot( delta, delta ), 0.000001 ), 0.0, 1.0 );
		vec2 relative = p - mix( starts[i], ends[i], t );
		float dist = dot( relative, relative );

		if ( dist < nearest )
		{
			nearest = dist;
			result.along = result.len + t * extent;
			result.normal = axes[i];
			result.across = dot( relative, result.normal );
		}

		result.len += extent;

		vec2 r = radii[i];

		if ( r.x > 0.0 && r.y > 0.0 )
		{
			vec2 nextAxis = axes[( i + 1 ) % 4];
			vec2 q = p - centres[i];
			vec2 local = vec2( dot( q, axes[i] ), dot( q, nextAxis ) );
			float angle = clamp( atan( local.y / r.y, local.x / r.x ), 0.0, 1.57079632679 );

			if ( r.x != r.y )
			{
				for ( int iteration = 0; iteration < 5; iteration++ )
				{
					float s = sin( angle );
					float c = cos( angle );
					float slope = ( r.y * r.y - r.x * r.x ) * s * c + r.x * local.x * s - r.y * local.y * c;
					float bend = ( r.y * r.y - r.x * r.x ) * ( c * c - s * s ) + r.x * local.x * c + r.y * local.y * s;

					if ( bend > 0.000001 ) angle = clamp( angle - clamp( slope / bend, -0.25, 0.25 ), 0.0, 1.57079632679 );
				}
			}

			vec2 radial = vec2( cos( angle ), sin( angle ) );
			vec2 onCurve = centres[i] + axes[i] * radial.x * r.x + nextAxis * radial.y * r.y;

			relative = p - onCurve;
			dist = dot( relative, relative );

			if ( dist < nearest )
			{
				nearest = dist;
				result.along = result.len + EllipseLength( r, angle );
				result.normal = Normalised( axes[i] * radial.x / r.x + nextAxis * radial.y / r.y );
				result.across = dot( relative, result.normal );
			}

			result.len += EllipseLength( r, 1.57079632679 );
		}
	}

	return result;
}

float Pulse( float x, float duty )
{
	return floor( x ) * duty + min( fract( x ), duty );
}

float BorderPattern( int style, float width, BorderContour contour )
{
	float target = width * ( style == BorderDotted ? 2.0 : 6.0 );
	float count = max( 1.0, floor( contour.len / max( target, 0.0001 ) + 0.5 ) );
	float period = max( contour.len / count, 0.0001 );
	float phase = fract( contour.along / period );

	if ( style == BorderDashed || width < 2.5 )
	{
		float footprint = 1.0 / period;
		float centre = phase + 0.25;

		return clamp( ( Pulse( centre + footprint * 0.5, 0.5 ) - Pulse( centre - footprint * 0.5, 0.5 ) ) / footprint, 0.0, 1.0 );
	}

	float along = ( fract( phase + 0.5 ) - 0.5 ) * period;
	vec2 tangent = vec2( -contour.normal.y, contour.normal.x );
	vec2 offset = tangent * along + contour.normal * contour.across;

	return 1.0 - smoothstep( -0.5, 0.5, length( offset ) - width * 0.5 );
}

vec4 BorderColour( vec2 pos, vec2 size, vec4 widths, uvec4 colours )
{
	vec4 has = step( 0.0001, widths );
	vec4 depth = vec4( pos.x, pos.y, size.x - pos.x, size.y - pos.y ) / max( widths, 0.0001 );
	depth = depth * has + ( 1.0 - has ) * 1e9;

	uint first = colours.x;
	uint second = colours.y;
	float d1 = depth.x;
	float d2 = depth.y;
	float w1 = widths.x;
	float w2 = widths.y;

	if ( d2 < d1 ) { first = colours.y; second = colours.x; d1 = depth.y; d2 = depth.x; w1 = widths.y; w2 = widths.x; }

	if ( depth.z < d1 ) { second = first; d2 = d1; w2 = w1; first = colours.z; d1 = depth.z; w1 = widths.z; }
	else if ( depth.z < d2 ) { second = colours.z; d2 = depth.z; w2 = widths.z; }

	if ( depth.w < d1 ) { second = first; d2 = d1; w2 = w1; first = colours.w; d1 = depth.w; w1 = widths.w; }
	else if ( depth.w < d2 ) { second = colours.w; d2 = depth.w; w2 = widths.w; }

	vec4 near = Unpacked( first );
	near.rgb = ToLinear( near.rgb );

	float fromCut = ( d2 - d1 ) * w1 * w2 / max( length( vec2( w1, w2 ) ), 0.0001 );
	float share = d2 > 1e8 ? 0.0 : clamp( 0.5 - fromCut, 0.0, 1.0 );

	if ( share <= 0.0 || first == second ) return near;

	vec4 far = Unpacked( second );
	far.rgb = ToLinear( far.rgb );

	return mix( near, far, share );
}

vec2 Turn( vec2 uv, float angle )
{
	if ( abs( angle ) < 0.0001 ) return uv;

	float s = sin( angle );
	float c = cos( angle );

	vec2 centred = uv - 0.5;
	return vec2( centred.x * c - centred.y * s, centred.x * s + centred.y * c ) + 0.5;
}

vec3 FromHsv( vec3 hsv )
{
	const vec4 k = vec4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );

	vec3 p = abs( fract( hsv.xxx + k.xyz ) * 6.0 - k.www );

	return hsv.z * mix( k.xxx, clamp( p - k.xxx, 0.0, 1.0 ), hsv.y );
}

void main()
{
	// The pixel this is, from the top left like the boxes; GL counts its rows from the bottom
	vec2 pixel = vec2( gl_FragCoord.x, u_screen.y - gl_FragCoord.y );

	// Derivatives before anything can discard or branch
	vec2 acrossRate = vec2( dFdx( v_uv.x ), dFdy( v_uv.x ) );
	vec2 endRate = vec2( dFdx( v_filter.x ), dFdy( v_filter.x ) );
	vec2 uvDx = dFdx( v_uv );
	vec2 uvDy = dFdy( v_uv );

	// Rounded overflow clip from ancestors (zero rect = none), in window pixels
	float clipped = 1.0;

	if ( v_clip.z > v_clip.x || v_clip.w > v_clip.y )
	{
		vec2 clipHalf = ( v_clip.zw - v_clip.xy ) * 0.5;

		clipped = 1.0 - smoothstep( -0.5, 0.5,
			RoundedBox( pixel - ( v_clip.xy + clipHalf ), clipHalf, v_clipRadii, VerticalRadii( v_clipRadii, v_vertical.zw ) ) );

		if ( clipped <= 0.0 ) discard;
	}

	// Vector shape triangle: antialias edge and cap distances to one screen pixel via derivatives
	if ( ( int( v_misc.y + 0.5 ) & 15 ) == 10 )
	{
		float across = max( length( acrossRate ), 0.00001 );
		float along = max( length( endRate ), 0.00001 );

		float cover = clamp( ( v_uv.y - abs( v_uv.x ) ) / across + 0.5, 0.0, 1.0 ) * clamp( v_filter.x / along + 0.5, 0.0, 1.0 );

		if ( cover <= 0.0 ) discard;

		o_colour = vec4( v_fill.rgb, v_fill.a * cover * clipped );
		return;
	}

	vec2 halfSize = v_rect.zw * 0.5;
	vec2 p = v_screen - ( v_rect.xy + halfSize );

	vec4 radiiV = VerticalRadii( v_radii, v_vertical.xy );
	float dist = RoundedBox( p, halfSize, v_radii, radiiV );

	int marks = int( v_misc.y + 0.5 );
	int flags = marks & 15;
	int borderStyle = ( marks >> 5 ) & 15;

	// A shadow is its own shape and its own edge
	if ( flags == 4 || flags == 5 )
	{
		bool inset = flags == 5;

		vec2 offset = v_filter.xy;
		float spread = inset ? -v_filter.z : v_filter.z;
		float softness = max( v_misc.w, 0.5 );

		vec2 shadowHalf = max( halfSize + spread, 0.0 );
		vec4 shadowRadii = max( v_radii + spread, 0.0 );
		vec4 shadowRadiiV = max( radiiV + spread, 0.0 );

		float shadowDistance = RoundedBox( p - offset, shadowHalf, shadowRadii, shadowRadiiV );

		float alpha = inset
			? smoothstep( -softness, softness, shadowDistance ) * ( 1.0 - smoothstep( -0.5, 0.5, dist ) )
			: ( 1.0 - smoothstep( -softness, softness, shadowDistance ) ) * smoothstep( -0.5, 0.5, dist );

		if ( alpha <= 0.0 ) discard;

		o_colour = vec4( v_fill.rgb, v_fill.a * alpha * clipped );
		return;
	}

	float coverage = 1.0 - smoothstep( -0.5, 0.5, dist );
	if ( coverage <= 0.0 ) discard;

	vec4 colour = v_fill;

	if ( flags == 1 || flags == 3 || flags == 6 )
	{
		vec2 uv = Turn( v_uv, v_misc.z );

		if ( flags == 6 && ( any( lessThan( uv, vec2( 0.0 ) ) ) || any( greaterThan( uv, vec2( 1.0 ) ) ) ) ) discard;

		if ( flags == 1 ) uv = clamp( uv, 0.0, 1.0 );

		if ( ( marks & 512 ) != 0 )
			colour *= textureGrad( u_color, PixelatedUv( uv ), uvDx, uvDy );
		else
			colour *= texture( u_color, uv );
	}
	else if ( flags == 7 )
	{
		// Letters are premultiplied in sRGB: unpremultiply before decoding to linear to avoid dark fringes
		vec4 ink = texture( u_color, clamp( v_uv, 0.0, 1.0 ) );

		if ( ink.a <= 0.0 ) discard;

		colour.rgb *= ToLinear( clamp( ink.rgb / ink.a, 0.0, 1.0 ) );

		// Coverage was cut for sRGB blending; a power curve picked by ink lightness corrects for linear
		float lightness = ToSrgb( clamp( dot( colour.rgb, vec3( 0.2126, 0.7152, 0.0722 ) ), 0.0, 1.0 ) );
		float cut = clamp( ( lightness - 0.15 ) / 0.5, 0.0, 1.0 );

		colour.a *= pow( ink.a, mix( 0.45, 1.8, cut ) );
	}
	else if ( flags == 8 )
	{
		colour.rgb = FromHsv( vec3( v_filter.x, v_uv.x, 1.0 - v_uv.y ) );
	}
	else if ( flags == 9 )
	{
		// A gradient: the picture is its colours along the gradient line, and where this pixel falls on it comes from the box
		vec2 local = v_screen - v_rect.xy;
		vec2 size = max( v_rect.zw, 1.0 );
		int type = int( v_filter2.x + 0.5 );

		float t;

		if ( type == 0 )
		{
			vec2 dir = vec2( sin( v_misc.z ), cos( v_misc.z ) );
			float len = abs( size.x * dir.x ) + abs( size.y * dir.y );
			t = dot( local - size * 0.5, dir ) / max( len, 0.0001 ) + 0.5;
		}
		else if ( type == 1 )
		{
			t = length( ( local - v_filter.xy ) / max( v_filter.zw, 0.0001 ) );
		}
		else
		{
			vec2 d = local - v_filter.xy;
			t = fract( ( atan( d.x, -d.y ) - v_misc.z ) / 6.28318530718 );
		}

		float texels = max( v_filter2.y, 1.0 );
		float u = ( clamp( t, 0.0, 1.0 ) * ( texels - 1.0 ) + 0.5 ) / texels;

		colour *= texture( u_color, vec2( u, 0.5 ) );
	}

	// Border ring composited over the fill, each side its own width: left, top, right, bottom
	vec4 w = v_filter;

	if ( all( lessThanEqual( w, vec4( 0.0 ) ) ) ) w = vec4( v_misc.x );

	if ( v_misc.x > 0.0 && dist > -( max( max( w.x, w.y ), max( w.z, w.w ) ) + 1.0 ) )
	{
		vec2 innerCentre = vec2( w.x - w.z, w.y - w.w ) * 0.5;
		vec2 innerHalf = max( halfSize - vec2( w.x + w.z, w.y + w.w ) * 0.5, 0.0 );

		vec2 innerTL = InnerCorner( v_radii.x, radiiV.x, w.x, w.y );
		vec2 innerTR = InnerCorner( v_radii.y, radiiV.y, w.z, w.y );
		vec2 innerBR = InnerCorner( v_radii.z, radiiV.z, w.z, w.w );
		vec2 innerBL = InnerCorner( v_radii.w, radiiV.w, w.x, w.w );

		float innerDistance = RoundedBox( p - innerCentre, innerHalf,
			vec4( innerTL.x, innerTR.x, innerBR.x, innerBL.x ),
			vec4( innerTL.y, innerTR.y, innerBR.y, innerBL.y ) );

		float insideInner = 1.0 - smoothstep( -0.5, 0.5, innerDistance );
		float ring = clamp( coverage - insideInner, 0.0, 1.0 );

		if ( ring > 0.0 )
		{
			vec2 corner = p + halfSize;
			vec4 side = BorderColour( corner, v_rect.zw, w, v_borders );

			float kept = 1.0;

			if ( borderStyle == BorderDotted || borderStyle == BorderDashed )
			{
				vec2 tl = HeldCorner( vec2( v_radii.x, radiiV.x ), halfSize );
				vec2 tr = HeldCorner( vec2( v_radii.y, radiiV.y ), halfSize );
				vec2 br = HeldCorner( vec2( v_radii.z, radiiV.z ), halfSize );
				vec2 bl = HeldCorner( vec2( v_radii.w, radiiV.w ), halfSize );

				BorderContour contour = Contour( corner, v_rect.zw, w, vec4( tl.x, tr.x, br.x, bl.x ), vec4( tl.y, tr.y, br.y, bl.y ) );

				kept = BorderPattern( borderStyle, max( max( w.x, w.y ), max( w.z, w.w ) ), contour );
			}

			float border = side.a * kept * ( coverage > 0.0001 ? clamp( ring / coverage, 0.0, 1.0 ) : 0.0 );

			float outside = border + colour.a * ( 1.0 - border );

			colour.rgb = outside <= 0.0001
				? colour.rgb
				: ( side.rgb * border + colour.rgb * colour.a * ( 1.0 - border ) ) / outside;

			colour.a = outside;
		}
	}

	colour.a *= coverage * clipped;
	o_colour = colour;
}
