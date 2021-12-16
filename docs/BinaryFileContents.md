#Mesh binary file order:

##header
int32 datatype =101 #not yet
int32 version = 1
int32 vertexCount
int32 normalsCount
int32 uvsCount
int32 indicesCount
int32 submeshCount

##vertices
	///foreach vertex
	single x
	single y
	single z

##normals
	///foreach normal
	single x
	single y
	single z
	
##uvs
	///foreach uv
	single x
	single y

##indices
	///foreach index
	int32 index
	
##submeshes
	///foreach submesh	(for unity setSubmesh with submeshDescriptor, baseVertex=0)
	int32 submeshid		(to be able to skip empty submeshes)
	int32 firstIndex			
	int32 indexCount
	int32 firstvertex
	int32 vertexcount
	

-------------------------------------------------------------

#Mesh metadata subobjects file order:

##header
int32 datatype =102 #not yet
int32 version = 1
int32 identifierCount

##indentifiers
	///foreach object
	string identifying-code (UTF-8)
	int32 firstIndex			
	int32 indexCount
	int32 firstvertex
	int32 vertexcount
	int32 submeshid