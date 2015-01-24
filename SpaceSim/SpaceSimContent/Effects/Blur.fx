 //float BlurDistance = 0.002f;
 float BlurDistance = 1.0f;
 sampler ColorMapSampler : register(s1);

 float4 PixelShaderFunction(float2 Tex: TEXCOORD0) : COLOR
 {
  float4 Color;

  // Get the texel from ColorMapSampler using a modified texture coordinate. This
  // gets the texels at the neighbour texels and adds it to Color.
  Color  = tex2D( ColorMapSampler, float2(Tex.x+BlurDistance, Tex.y+BlurDistance));
  Color += tex2D( ColorMapSampler, float2(Tex.x-BlurDistance, Tex.y-BlurDistance));
  Color += tex2D( ColorMapSampler, float2(Tex.x+BlurDistance, Tex.y-BlurDistance));
  Color += tex2D( ColorMapSampler, float2(Tex.x-BlurDistance, Tex.y+BlurDistance));
  // We need to devide the color with the amount of times we added
  // a color to it, in this case 4, to get the avg. color
  Color = Color / 4; 

  // returned the blurred color
  return Color;
 }

 technique Blur
 {
  pass Pass1
  {
   PixelShader = compile ps_2_0 PixelShaderFunction();
  }
 }